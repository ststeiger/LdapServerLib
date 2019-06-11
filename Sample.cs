#if DEBUG
using Sys = global::System;
using SysClG = global::System.Collections.Generic;
using SysService = global::System.ServiceProcess;
using LDap = global::Libs.LDAP;
using LServ = global::Libs.Service;
using LSam = global::Sample;
namespace Sample
{
    internal class UserData : LDap.IUserData //testing purposes only
    {
        public long UserID { get; set; }
        public string UserName { get; set; }
        string LDap.INamed.Name { get { return this.UserName; } }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get { return (this.FirstName + " " + this.LastName); } }
        public string EMail { get; set; }
        public LDap.IGroup Department { get; set; }
        public string Job { get; set; }
        public string Mobile { get; set; }
        internal string Password { get; set; }
        public virtual bool TestPassword(string Password) { return (this.Password == Password); }

        public UserData(long UserID, string UserName, string EMail, string FirstName, string LastName, string Password)
        {
            this.UserID = UserID;
            this.UserName = UserName;
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.EMail = EMail;
            this.Password = Password;
        }

        public UserData(long UserID, string UserName, string EMail, string Password) : this(UserID, UserName, EMail, string.Empty, string.Empty, Password) { /* NOTHING */ }
        public UserData(long UserID, string UserName, string Password) : this(UserID, UserName, (UserName == null ? string.Empty : (UserName.Contains("@") ? UserName : string.Empty)), string.Empty, string.Empty, Password) { /* NOTHING */ }
    }

    internal class GroupData : LDap.IGroup //testing purposes only
    {
        private LSam.TestSource RootSource;
        private string nm;
        public LDap.IGroup Parent { get; protected set; }
        public SysClG.List<LSam.GroupData> Subgroups { get; protected set; }
        public string Name { get { return this.nm; } set { this.nm = (value == null ? "n" : value.ToLower()); } }
        SysClG.IEnumerable<LDap.IGroup> LDap.IDataList.ListGroups() { foreach (LSam.GroupData grp in this.Subgroups) { yield return grp; } }
        SysClG.IEnumerable<LDap.IUserData> LDap.IDataList.ListUsers() { foreach (LDap.IUserData user in this.RootSource.users) { if (user.Department == this) { yield return user; } } }
        string LDap.IGroup.BuildCN() { return "ou=" + this.nm + "," + (this.Parent == null ? (this.RootSource as LDap.IGroup) : this.Parent).BuildCN(); }

        public GroupData(LSam.TestSource RootSource, LDap.IGroup Parent, string Name)
        {
            this.RootSource = RootSource;
            this.Parent = Parent;
            this.Name = Name;
            this.Subgroups = new SysClG.List<LSam.GroupData>(1);
        }
    }

    internal class TestSource : LDap.IDataSource //testing purposes only | See also http://tldp.org/HOWTO/archived/LDAP-Implementation-HOWTO/schemas.html
    {
        public LDap.Domain Domain { get; protected set; }
        public string AdminUser { get { return "admin"; } }
        public string AdminPassword { get { return "1234"; } }
        internal SysClG.List<LDap.IUserData> users;
        internal SysClG.List<LDap.IGroup> groups;
        string LDap.INamed.Name { get { return this.Domain.NormalizedDC; } }
        SysClG.IEnumerable<LDap.IUserData> LDap.IDataList.ListUsers() { return this.users; } //the idea here is if you call from root then it will list all users (from any group)
        SysClG.IEnumerable<LDap.IGroup> LDap.IDataList.ListGroups() { return this.groups; }
        string LDap.IGroup.BuildCN() { return this.Domain.ToString(); }

        public bool Validate(string UserName, string Password, out bool IsAdmin)
        {
            if (UserName == this.AdminUser)
            {
                IsAdmin = (Password == this.AdminPassword);
                return IsAdmin;
            }
            else
            {
                IsAdmin = false;
                foreach (LDap.IUserData user in this.users) { if (user.Name == UserName) { return user.TestPassword(Password); } }
                return false;
            }
        }

        public TestSource()
        {
            this.Domain = new LDap.Domain(this) { Company = new LDap.Company() { Name = "Nazarick Inc.", Phone = "+9900900000000", Country = "Baharuth", State = "E-Rantel", City = "Nazarick", PostCode = "12123123", Address = "An Adress of" }, DomainCommon = "Com" };
            this.groups = new SysClG.List<LDap.IGroup>(3);
            LSam.GroupData mGP = new LSam.GroupData(this, null, "Nazarick Mausoleum");
            this.groups.Add(mGP);
            this.users = new SysClG.List<LDap.IUserData>(4);
            this.users.Add(new LSam.UserData(1L, "ainz.ooal.gown", "ainzsama@nazarick.com", "Ainz", "Ooal Gown", this.AdminPassword) { Department = mGP, Job = "Overlord", Mobile = "+9900900000099" });
            LSam.GroupData sGP = new LSam.GroupData(this, mGP, "Base Floors");
            mGP.Subgroups.Add(sGP);
            this.users.Add(new LSam.UserData(2L, "shalltear.bff", "shalltear@nazarick.com", "Shalltear", "Bloodfallen", this.AdminPassword) { Department = sGP, Job = "Guardian" });
            sGP = new LSam.GroupData(this, mGP, "Floor 10");
            mGP.Subgroups.Add(sGP);
            this.users.Add(new LSam.UserData(3L, "narberal", "narberal@nazarick.com", "Narberal", "Gamma", this.AdminPassword) { Department = sGP, Job = "Pleiade" });
            this.users.Add(new LSam.UserData(4L, "sebas.tian", "sebas@nazarick.com", "Sebas", "Tian", this.AdminPassword) { Department = sGP, Job = "Buttler" });
        }
    }

    public sealed class Service : LServ.ServiceInstaller
    {
        public const string ServiceName = "SampleLDAP_C";
        public const string ServiceDescription = "Sample of a Service implementation, hosting a LDAP Server";
        private static void Process(string[] args) { LServ.ServiceInstaller.Process(LSam.Service.ServiceName, new SysClG.List<LServ.IServer>() { new LDap.Server(new LSam.TestSource(), "127.0.0.1", 389) }, args); }

        internal static int Main(string[] args)
        {
            //const int UF_ACCOUNTDISABLE = 0x0002; //Get all User Account Info
            //SysClG.List<string> lstUsers = new SysClG.List<string>();
            //Sys.DirectoryServices.DirectoryEntry localMachine = new Sys.DirectoryServices.DirectoryEntry("WinNT://" + Sys.Environment.MachineName);
            //foreach (Sys.DirectoryServices.DirectoryEntry e in localMachine.Children)
            //{
                //if (e.SchemaClassName == "User")
                //{
                    //lstUsers.Add(e.Name + ((((int)e.Properties["UserFlags"].Value & UF_ACCOUNTDISABLE) != UF_ACCOUNTDISABLE) ? " - Active" : " - Inactive"));
                    //foreach (Sys.DirectoryServices.PropertyValueCollection p in e.Properties) { lstUsers.Add("> " + p.PropertyName + ":" + p.Value.ToString()); }
                //}
            //}
            //return lstUsers.Count;
            LSam.Service.Process(args);
            return 0;
        }

        public Service() : base(LSam.Service.ServiceName, ServiceDescription: LSam.Service.ServiceDescription) { /* NOTHING */ }
    }
}
#endif
