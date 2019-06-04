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
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get { return (this.FirstName + " " + this.LastName); } }
        public string EMail { get; set; }
        public string Department { get; set; }
        public string Job { get; set; }
        public string Mobile { get; set; }
        internal string Password { get; set; }
        public virtual bool TestPassword(string Password) { return (this.Password == Password); }

        public UserData(string UserName, string EMail, string FirstName, string LastName, string Password)
        {
            this.UserName = UserName;
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.EMail = EMail;
        }

        public UserData(string UserName, string EMail, string Password) : this(UserName, EMail, string.Empty, string.Empty, Password) { /* NOTHING */ }
        public UserData(string UserName, string Password) : this(UserName, (UserName == null ? string.Empty : (UserName.Contains("@") ? UserName : string.Empty)), string.Empty, string.Empty, Password) { /* NOTHING */ }
    }

    internal class Company : LDap.ICompany //testing purposes only
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string Address { get; set; }
    }

    internal class TestSource : LDap.IDataSource //testing purposes only | See also http://tldp.org/HOWTO/archived/LDAP-Implementation-HOWTO/schemas.html
    {
        public LDap.ICompany Company { get; protected set; }
        public string LDAPRoot { get; protected set; }
        public string AdminUser { get { return "admin"; } }
        public string AdminPassword { get { return "1234"; } }

        public SysClG.IEnumerable<LDap.IUserData> ListUsers()
        {
            yield return new LSam.UserData("ainz.ooal.gown", "ainzsama@nazarick.com", "Ainz", "Ooal Gown", this.AdminPassword) { Department = "Nazarick Mausoleum", Job = "Overlord", Mobile = "+9900900000099" };
            yield return new LSam.UserData("shalltear.bff", "shalltear@nazarick.com", "Shalltear", "Bloodfallen", this.AdminPassword) { Department = "Base Floors", Job = "Guardian" };
            yield return new LSam.UserData("narberal", "narberal@nazarick.com", "Narberal", "Gamma", this.AdminPassword) { Department = "Floor 10", Job = "Pleiade" };
            yield return new LSam.UserData("sebas.tian", "sebas@nazarick.com", "Sebas", "Tian", this.AdminPassword) { Department = "Floor 10", Job = "Buttler" };
        }

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
                foreach (LDap.IUserData user in this.ListUsers()) { if (user.UserName == UserName) { return user.TestPassword(Password); } }
                return false;
            }
        }

        public TestSource()
        {
            this.Company = new LSam.Company() { Name = "Nazarick Inc.", Phone = "+9900900000000", Country = "Baharuth", State = "E-Rantel", City = "Nazarick", PostCode = "12123123", Address = "An Adress of" };
            this.LDAPRoot = "cn=Users,dc=" + this.Company.Name.Replace(' ', '_').Replace('.', '_').Replace('\t', '_').Replace(',', '_') + ",dc=com"; 
        }
    }

    public sealed class Service : LServ.ServiceInstaller
    {
        public const string ServiceName = "SampleLDAP_C";
        public const string ServiceDescription = "Sample of a Service implementation, hosting a LDAP Server";

        internal static int Main(string[] args)
        {
            LServ.ServiceInstaller.Process(LSam.Service.ServiceName, new SysClG.List<LServ.IServer>() { new LDap.Server(new LSam.TestSource(), "127.0.0.1", 389) }, args);
            return 0;
        }

        public Service() : base(LSam.Service.ServiceName, ServiceDescription: LSam.Service.ServiceDescription) { /* NOTHING */ }
    }
}
#endif