//https://docs.iredmail.org/use.openldap.as.address.book.in.outlook.html
namespace Libs.LDAP
{
    namespace Core //https://github.com/vforteli/Flexinets.Ldap.Core
    {
        internal delegate bool Verify<T>(T obj);

        internal static class Utils
        {
            public static byte[] StringToByteArray(string hex, bool trimWhitespace = true)
            {
                if (trimWhitespace) { hex = hex.Replace(" ", ""); }
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2) { bytes[i / 2] = global::System.Convert.ToByte(hex.Substring(i, 2), 16); }
                return bytes;
            }

            public static string ByteArrayToString(byte[] bytes)
            {
                global::System.Text.StringBuilder hex = new global::System.Text.StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) { hex.Append(b.ToString("X2")); }
                return hex.ToString();
            }

            public static string BitsToString(global::System.Collections.BitArray bits)
            {
                int i = 1;
                string derp = string.Empty;
                foreach (object bit in bits)
                {
                    derp += global::System.Convert.ToInt32(bit);
                    if (i % 8 == 0) { derp += " "; }
                    i++;
                }
                return derp.Trim();
            }

            public static byte[] IntToBerLength(int length) //https://en.wikipedia.org/wiki/X.690#BER_encoding
            {
                if (length <= 127) { return new byte[] { (byte)length }; }
                else
                {
                    byte[] intbytes = global::System.BitConverter.GetBytes(length);
                    byte intbyteslength = (byte)intbytes.Length;
                    while (intbyteslength >= 0)
                    {
                        intbyteslength--;
                        if (intbytes[intbyteslength - 1] != 0) { break; }
                    }
                    int lengthByte = intbyteslength + 128;
                    byte[] berBytes = new byte[1 + intbyteslength];
                    berBytes[0] = (byte)lengthByte;
                    global::System.Buffer.BlockCopy(intbytes, 0, berBytes, 1, intbyteslength);
                    return berBytes;
                }
            }

            public static TObject[] Reverse<TObject>(global::System.Collections.Generic.IEnumerable<TObject> enumerable)
            {
                global::System.Collections.Generic.List<TObject> acum = new global::System.Collections.Generic.List<TObject>(10);
                foreach (TObject obj in enumerable)
                {
                    if (acum.Count == acum.Capacity) { acum.Capacity += 10; }
                    acum.Add(obj);
                }
                acum.Reverse();
                return acum.ToArray();
            }

            public static bool Any<T>(global::System.Collections.Generic.IEnumerable<T> enumerator, global::Libs.LDAP.Core.Verify<T> verifier) { foreach (T obj in enumerator) { if (verifier(obj)) { return true; } } return false; }
            public static T SingleOrDefault<T>(global::System.Collections.Generic.IEnumerable<T> enumerator, global::Libs.LDAP.Core.Verify<T> verifier) { foreach (T obj in enumerator) { if (verifier(obj)) { return obj; } } return default(T); }

            private sealed class ArraySegmentEnumerator<T> : global::System.Collections.Generic.IEnumerator<T>, global::System.Collections.Generic.IEnumerable<T> //https://referencesource.microsoft.com/#mscorlib/system/arraysegment.cs,9b6becbc5eb6a533
            {
                private T[] _array;
                private int _start;
                private int _end;
                private int _current;

                public bool MoveNext()
                {
                    if (this._current < this._end)
                    {
                        this._current++;
                        return (this._current < this._end);
                    }
                    return false;
                }

                public T Current { get { if (this._current < this._start) throw new global::System.InvalidOperationException(); else if (this._current >= this._end) throw new global::System.InvalidOperationException(); else return this._array[this._current]; } }
                global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator() { return this; }
                global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() { return this; }
                object global::System.Collections.IEnumerator.Current { get { return this.Current; } }
                void global::System.Collections.IEnumerator.Reset() { this._current = this._start - 1; }
                void global::System.IDisposable.Dispose() { /* NOTHING */ }

                internal ArraySegmentEnumerator(T[] Array, int Start, int Count)
                {
                    this._array = Array;
                    this._start = Start;
                    this._end = this._start + Count;
                    this._current = this._start - 1;
                }
            }

            public static int BerLengthToInt(byte[] bytes, int offset, out int berByteCount)
            {
                berByteCount = 1;
                int attributeLength = 0;
                if (bytes[offset] >> 7 == 1)
                {
                    int lengthoflengthbytes = bytes[offset] & 127;
                    attributeLength = global::System.BitConverter.ToInt32(global::Libs.LDAP.Core.Utils.Reverse<byte>(new global::Libs.LDAP.Core.Utils.ArraySegmentEnumerator<byte>(bytes, offset + 1, lengthoflengthbytes)), 0);
                    berByteCount += lengthoflengthbytes;
                } else { attributeLength = bytes[offset] & 127; }
                return attributeLength;
            }

            public static int BerLengthToInt(global::System.IO.Stream stream, out int berByteCount)
            {
                berByteCount = 1;
                int attributeLength = 0;
                byte[] berByte = new byte[1];
                stream.Read(berByte, 0, 1);
                if (berByte[0] >> 7 == 1)
                {
                    int lengthoflengthbytes = berByte[0] & 127;
                    byte[] lengthBytes = new byte[lengthoflengthbytes];
                    stream.Read(lengthBytes, 0, lengthoflengthbytes);
                    attributeLength = global::System.BitConverter.ToInt32(global::Libs.LDAP.Core.Utils.Reverse<byte>(lengthBytes), 0);
                    berByteCount += lengthoflengthbytes;
                } else { attributeLength = berByte[0] & 127; }
                return attributeLength;
            }

            public static string Repeat(string stuff, int n)
            {
                global::System.Text.StringBuilder concat = new global::System.Text.StringBuilder(stuff.Length * n);
                for (int i = 0; i < n; i++) { concat.Append(stuff); }
                return concat.ToString();
            }
        }

        public enum LdapFilterChoice : byte
        {
            and = 0,
            or = 1,
            not = 2,
            equalityMatch = 3,
            substrings = 4,
            greaterOrEqual = 5,
            lessOrEqual = 6,
            present = 7,
            approxMatch = 8,
            extensibleMatch = 9
        }

        public enum LdapOperation : byte
        {
            BindRequest = 0,
            BindResponse = 1,
            UnbindRequest = 2,
            SearchRequest = 3,
            SearchResultEntry = 4,
            SearchResultDone = 5,
            SearchResultReference = 19,
            ModifyRequest = 6,
            ModifyResponse = 7,
            AddRequest = 8,
            AddResponse = 9,
            DelRequest = 10,
            DelResponse = 11,
            ModifyDNRequest = 12,
            ModifyDNResponse = 13,
            CompareRequest = 14,
            CompareResponse = 15,
            AbandonRequest = 16,
            ExtendedRequest = 23,
            ExtendedResponse = 24,
            IntermediateResponse = 25,
            NONE = 255 //SAMMUEL
        }

        public enum LdapResult : byte //https://tools.ietf.org/html/rfc4511
        {
            success = 0,
            operationError = 1,
            protocolError = 2,
            timeLimitExceeded = 3,
            sizeLimitExceeded = 4,
            compareFalse = 5,
            compareTrue = 6,
            authMethodNotSupported = 7,
            strongerAuthRequired = 8, // 9 reserved --
            referral = 10,
            adminLimitExceeded = 11,
            unavailableCriticalExtension = 12,
            confidentialityRequired = 13,
            saslBindInProgress = 14,
            noSuchAttribute = 16,
            undefinedAttributeType = 17,
            inappropriateMatching = 18,
            constraintViolation = 19,
            attributeOrValueExists = 20,
            invalidAttributeSyntax = 21, // 22-31 unused --
            noSuchObject = 32,
            aliasProblem = 33,
            invalidDNSyntax = 34, // 35 reserved for undefined isLeaf --
            aliasDereferencingProblem = 36, // 37-47 unused --
            inappropriateAuthentication = 48,
            invalidCredentials = 49,
            insufficientAccessRights = 50,
            busy = 51,
            unavailable = 52,
            unwillingToPerform = 53,
            loopDetect = 54, // 55-63 unused --
            namingViolation = 64,
            objectClassViolation = 65,
            notAllowedOnNonLeaf = 66,
            notAllowedOnRDN = 67,
            entryAlreadyExists = 68,
            objectClassModsProhibited = 69, // 70 reserved for CLDAP --
            affectsMultipleDSAs = 71, // 72-79 unused --
            other = 80
        }

        public enum TagClass : byte
        {
            Universal = 0,
            Application = 1,
            Context = 2,
            Private = 3
        }

        public enum UniversalDataType : byte //Universal data types from https://en.wikipedia.org/wiki/X.690#BER_encoding
        {
            EndOfContent = 0,
            Boolean = 1,
            Integer = 2,
            BitString = 3,
            OctetString = 4,
            Null = 5,
            ObjectIdentifier = 6,
            ObjectDescriptor = 7,
            External = 8,
            Real = 9,
            Enumerated = 10,
            EmbeddedPDV = 11,
            UTF8String = 12,
            Relative = 13,
            Reserved = 14,
            Reserved2 = 15,
            Sequence = 16,
            Set = 17,
            NumericString = 18,
            PrintableString = 19,
            T61String = 20,
            VideotexString = 21,
            IA5String = 22,
            UTCTime = 23,
            GeneralizedTime = 24,
            GraphicString = 25,
            VisibleString = 26,
            GeneralString = 27,
            UniversalString = 28,
            CharacterString = 29,
            BMPString = 30,
            NONE = 255 //SAMMUEL (not in protocol - never use it!)
        }

        public class Tag
        {
            public byte TagByte { get; internal set; }
            public global::Libs.LDAP.Core.TagClass Class { get { return (global::Libs.LDAP.Core.TagClass)(this.TagByte >> 6); } }
            public global::Libs.LDAP.Core.UniversalDataType DataType { get { return this.Class == global::Libs.LDAP.Core.TagClass.Universal ? (global::Libs.LDAP.Core.UniversalDataType)(this.TagByte & 31) : global::Libs.LDAP.Core.UniversalDataType.NONE; } }
            public global::Libs.LDAP.Core.LdapOperation LdapOperation { get { return this.Class == global::Libs.LDAP.Core.TagClass.Application ? (global::Libs.LDAP.Core.LdapOperation)(this.TagByte & 31) : global::Libs.LDAP.Core.LdapOperation.NONE; } }
            public byte? ContextType { get { return this.Class == global::Libs.LDAP.Core.TagClass.Context ? (byte?)(this.TagByte & 31) : null; } }
            public static global::Libs.LDAP.Core.Tag Parse(byte tagByte) { return new global::Libs.LDAP.Core.Tag { TagByte = tagByte }; }
            public override string ToString() { return "Tag[class=" + this.Class.ToString() + ",datatype=" + this.DataType.ToString() + ",ldapoperation=" + this.LdapOperation.ToString() + ",contexttype=" + (this.ContextType == null ? "NULL" : ((global::Libs.LDAP.Core.LdapFilterChoice)this.ContextType).ToString()) + "]"; }

            public bool IsConstructed
            {
                get { return new global::System.Collections.BitArray(new byte[] { this.TagByte }).Get(5); }
                set
                {
                    global::System.Collections.BitArray foo = new global::System.Collections.BitArray(new byte[] { this.TagByte });
                    foo.Set(5, value);
                    byte[] temp = new byte[1];
                    foo.CopyTo(temp, 0);
                    this.TagByte = temp[0];
                }
            }

            private Tag() { /* NOTHING */ }
            public Tag(global::Libs.LDAP.Core.LdapOperation operation) { TagByte = (byte)((byte)operation + ((byte)global::Libs.LDAP.Core.TagClass.Application << 6)); }
            public Tag(global::Libs.LDAP.Core.UniversalDataType dataType) { TagByte = (byte)(dataType + ((byte)global::Libs.LDAP.Core.TagClass.Universal << 6)); }
            public Tag(byte context) { TagByte = (byte)(context + ((byte)global::Libs.LDAP.Core.TagClass.Context << 6)); }
        }

        public class LdapAttribute : global::System.IDisposable
        {
            private global::Libs.LDAP.Core.Tag _tag;
            protected byte[] Value = new byte[0];
            public global::System.Collections.Generic.List<global::Libs.LDAP.Core.LdapAttribute> ChildAttributes = new global::System.Collections.Generic.List<global::Libs.LDAP.Core.LdapAttribute>();
            public global::Libs.LDAP.Core.TagClass Class { get { return this._tag.Class; } }
            public bool IsConstructed { get { return (this._tag.IsConstructed || this.ChildAttributes.Count > 0); } }
            public global::Libs.LDAP.Core.LdapOperation LdapOperation { get { return this._tag.LdapOperation; } }
            public global::Libs.LDAP.Core.UniversalDataType DataType { get { return this._tag.DataType; } }
            public byte? ContextType { get { return this._tag.ContextType; } }

            public object GetValue()
            {
                if (this._tag.Class == global::Libs.LDAP.Core.TagClass.Universal)
                {
                    switch (this._tag.DataType)
                    {
                        case global::Libs.LDAP.Core.UniversalDataType.Boolean: return global::System.BitConverter.ToBoolean(this.Value, 0);
                        case global::Libs.LDAP.Core.UniversalDataType.Integer:
                            byte[] intbytes = new byte[4];
                            global::System.Buffer.BlockCopy(this.Value, 0, intbytes, 4 - this.Value.Length, this.Value.Length);
                            global::System.Array.Reverse(intbytes);
                            return global::System.BitConverter.ToInt32(intbytes, 0);
                        default: return global::System.Text.Encoding.UTF8.GetString(this.Value, 0, this.Value.Length);
                    }
                }
                return global::System.Text.Encoding.UTF8.GetString(Value, 0, Value.Length);
            }

            private byte[] GetBytes(object val)
            {
                global::System.Type typeOFval = val.GetType();
                if (typeOFval == typeof(string)) { return global::System.Text.Encoding.UTF8.GetBytes(val as string); }
                else if (typeOFval == typeof(int)) { return global::Libs.LDAP.Core.Utils.Reverse<byte>(global::System.BitConverter.GetBytes((int)val)); }
                else if (typeOFval == typeof(byte)) { return new byte[] { (byte)val }; }
                else if (typeOFval == typeof(bool)) { return global::System.BitConverter.GetBytes((bool)val); }
                else if (typeOFval == typeof(byte[])) { return (val as byte[]); }
                else { throw new global::System.InvalidOperationException("Nothing found for " + typeOFval); }
            }

            public override string ToString() { return this._tag.ToString() + ",Value={" + ((this.Value == null || this.Value.Length == 0) ? "\"\"" : global::System.Text.Encoding.UTF8.GetString(this.Value)) + "},attr=" + this.ChildAttributes.Count.ToString(); }
            public T GetValue<T>() { return (T)global::System.Convert.ChangeType(this.GetValue(), typeof(T)); }

            public byte[] GetBytes()
            {
                global::System.Collections.Generic.List<byte> contentBytes = new global::System.Collections.Generic.List<byte>();
                if (ChildAttributes.Count > 0)
                {
                    this._tag.IsConstructed = true;
                    foreach (global::Libs.LDAP.Core.LdapAttribute attr in this.ChildAttributes) { contentBytes.AddRange(attr.GetBytes()); }
                } else { contentBytes.AddRange(Value); }
                global::System.Collections.Generic.List<byte> ret = new System.Collections.Generic.List<byte>(1);
                ret.Add(this._tag.TagByte);
                ret.AddRange(global::Libs.LDAP.Core.Utils.IntToBerLength(contentBytes.Count));
                ret.Capacity += contentBytes.Count;
                ret.AddRange(contentBytes);
                contentBytes.Clear();
                contentBytes = null;
                return ret.ToArray();
            }

            public virtual void Dispose()
            {
                this.Value = null;
                foreach (global::Libs.LDAP.Core.LdapAttribute attr in this.ChildAttributes) { attr.Dispose(); }
                this.ChildAttributes.Clear();
            }

            protected static global::System.Collections.Generic.List<global::Libs.LDAP.Core.LdapAttribute> ParseAttributes(byte[] bytes, int currentPosition, int length)
            {
                global::System.Collections.Generic.List<global::Libs.LDAP.Core.LdapAttribute> list = new global::System.Collections.Generic.List<global::Libs.LDAP.Core.LdapAttribute>();
                while (currentPosition < length)
                {
                    global::Libs.LDAP.Core.Tag tag = global::Libs.LDAP.Core.Tag.Parse(bytes[currentPosition]);
                    currentPosition++;
                    int i = 0;
                    int attributeLength = global::Libs.LDAP.Core.Utils.BerLengthToInt(bytes, currentPosition, out i);
                    currentPosition += i;
                    global::Libs.LDAP.Core.LdapAttribute attribute = new global::Libs.LDAP.Core.LdapAttribute(tag);
                    if (tag.IsConstructed && attributeLength > 0) { attribute.ChildAttributes = ParseAttributes(bytes, currentPosition, currentPosition + attributeLength); }
                    else
                    {
                        attribute.Value = new byte[attributeLength];
                        global::System.Buffer.BlockCopy(bytes, currentPosition, attribute.Value, 0, attributeLength);
                    }
                    list.Add(attribute);
                    currentPosition += attributeLength;
                }
                return list;
            }

            protected LdapAttribute(global::Libs.LDAP.Core.Tag tag) { this._tag = tag; }
            public LdapAttribute(global::Libs.LDAP.Core.LdapOperation operation) { this._tag = new global::Libs.LDAP.Core.Tag(operation); }
            public LdapAttribute(global::Libs.LDAP.Core.LdapOperation operation, object value) : this(operation) { this.Value = this.GetBytes(value); }
            public LdapAttribute(global::Libs.LDAP.Core.UniversalDataType dataType) { this._tag = new global::Libs.LDAP.Core.Tag(dataType); }
            public LdapAttribute(global::Libs.LDAP.Core.UniversalDataType dataType, object value) : this(dataType) { this.Value = this.GetBytes(value); }
            public LdapAttribute(byte contextType) { this._tag = new global::Libs.LDAP.Core.Tag(contextType); }
            public LdapAttribute(byte contextType, object value) : this(contextType) { this.Value = this.GetBytes(value); }
        }

        public class LdapResultAttribute : global::Libs.LDAP.Core.LdapAttribute
        {
            public LdapResultAttribute(global::Libs.LDAP.Core.LdapOperation operation, global::Libs.LDAP.Core.LdapResult result, string matchedDN = "", string diagnosticMessage = "") : base(operation)
            {
                this.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.Enumerated, (byte)result));
                this.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.OctetString, matchedDN));
                this.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.OctetString, diagnosticMessage));
            }
        }

        public class LdapPacket : global::Libs.LDAP.Core.LdapAttribute
        {
            public int MessageId { get { return this.ChildAttributes[0].GetValue<int>(); } }

            public static global::Libs.LDAP.Core.LdapPacket ParsePacket(byte[] bytes)
            {
                global::Libs.LDAP.Core.LdapPacket packet = new global::Libs.LDAP.Core.LdapPacket(global::Libs.LDAP.Core.Tag.Parse(bytes[0]));
                int lengthBytesCount = 0;
                int contentLength = global::Libs.LDAP.Core.Utils.BerLengthToInt(bytes, 1, out lengthBytesCount);
                packet.ChildAttributes.AddRange(global::Libs.LDAP.Core.LdapAttribute.ParseAttributes(bytes, 1 + lengthBytesCount, contentLength));
                return packet;
            }

            public static bool TryParsePacket(global::System.IO.Stream stream, out global::Libs.LDAP.Core.LdapPacket packet)
            {
                try
                {
                    if (stream.CanRead)
                    {
                        byte[] tagByte = new byte[1];
                        int i = stream.Read(tagByte, 0, 1);
                        if (i != 0)
                        {
                            int n = 0;
                            int contentLength = global::Libs.LDAP.Core.Utils.BerLengthToInt(stream, out n);
                            byte[] contentBytes = new byte[contentLength];
                            stream.Read(contentBytes, 0, contentLength);
                            packet = new global::Libs.LDAP.Core.LdapPacket(global::Libs.LDAP.Core.Tag.Parse(tagByte[0]));
                            packet.ChildAttributes.AddRange(global::Libs.LDAP.Core.LdapAttribute.ParseAttributes(contentBytes, 0, contentLength));
                            return true;
                        }
                    }
                } catch { /* NOTHING */ }
                packet = null;
                return false;
            }

            private LdapPacket(global::Libs.LDAP.Core.Tag tag) : base(tag) { /* NOTHING */ }
            public LdapPacket(int messageId) : base(global::Libs.LDAP.Core.UniversalDataType.Sequence) { this.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.Integer, messageId)); }
        }
    }

    public interface IUserData
    {
        string UserName { get; }
        string FirstName { get; }
        string LastName { get; }
        string FullName { get; }
        string EMail { get; }
        string Department { get; }
        string Job { get; }
        bool TestPassword(string Password);
    }

    public interface ICompany
    {
        string Name { get; }
        string Phone { get; }
        string Country { get; }
        string State { get; }
        string City { get; }
        string PostCode { get; }
        string Address { get; }
    }

    public interface IDataSource
    {
        string AdminUser { get; }
        string AdminPassword { get; }
        string LDAPRoot { get; }
        global::Libs.LDAP.ICompany Company { get; }
        bool Validate(string UserName, string Password, out bool IsAdmin);
        global::System.Collections.Generic.IEnumerable<global::Libs.LDAP.IUserData> ListUsers();
    }

    internal class UserData : global::Libs.LDAP.IUserData //testing purposes only
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get { return (this.FirstName + " " + this.LastName); } }
        public string EMail { get; set; }
        public string Department { get; set; }
        public string Job { get; set; }
        public virtual bool TestPassword(string Password) { /* //'... here! */ return true; }

        public UserData(string UserName, string EMail, string FirstName, string LastName)
        {
            this.UserName = UserName;
            this.FirstName = FirstName;
            this.LastName = LastName;
            this.EMail = EMail;
        }

        public UserData(string UserName, string EMail) : this(UserName, EMail, string.Empty, string.Empty) { /* NOTHING */ }
        public UserData(string UserName) : this(UserName, (UserName == null ? string.Empty : (UserName.Contains("@") ? UserName : string.Empty)), string.Empty, string.Empty) { /* NOTHING */ }
    }

    internal class Company : global::Libs.LDAP.ICompany //testing purposes only
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string Address { get; set; }
    }

    internal class TestSource : global::Libs.LDAP.IDataSource //testing purposes only
    {
        public global::Libs.LDAP.ICompany Company { get; protected set; }
        public string LDAPRoot { get { return "cn=Users,dc=dev,dc=company,dc=com"; } }
        public string AdminUser { get { return "admin"; } }
        public string AdminPassword { get { return "3462EB3"; } }

        public global::System.Collections.Generic.IEnumerable<global::Libs.LDAP.IUserData> ListUsers()
        {
            yield return new global::Libs.LDAP.UserData("username1", "add.user1@company.com", "nm1", "sn1") { Department = "Fictional 1", Job = "Director" };
            yield return new global::Libs.LDAP.UserData("username2", "add.user2@company.com", "nm2", "sn2") { Department = "Fictional 2", Job = "Lacky" };
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
                foreach (global::Libs.LDAP.IUserData user in this.ListUsers()) { if (user.UserName == UserName) { return user.TestPassword(Password); } }
                return false;
            }
        }

        public TestSource() { this.Company = new global::Libs.LDAP.Company() { Name = "company", Phone = "+5500900000000", Country = "ACountry", State = "STT", City = "CityOfCom", PostCode = "10200300", Address = "An Adress of" }; }
    }

    public class Server //https://github.com/vforteli/Flexinets.Ldap.Server/blob/master/LdapServer.cs 
    {
        public const int StandardPort = 389;
        public const string PosixAccount = "PosixAccount";
        public const string AMAccount = "sAMAccountName";
        private readonly global::System.Net.Sockets.TcpListener _server;
        private global::Libs.LDAP.IDataSource _validator;
        public global::Libs.LDAP.IDataSource Validator { get { return this._validator; } }
        private bool IsValidType(string type) { return (type == "objectClass" || type == global::Libs.LDAP.Server.PosixAccount || type == global::Libs.LDAP.Server.AMAccount); }
        public void Stop() { if (this._server != null) { this._server.Stop(); } }

        public void Start()
        {
            this._server.Start();
            this._server.BeginAcceptTcpClient(this.OnClientConnect, null);
        }

        private void AddAttribute(global::Libs.LDAP.Core.LdapAttribute partialAttributeList, string AttributeName, params string[] AttributeValues)
        {
            if (AttributeValues != null && AttributeValues.Length > 0)
            {
                global::Libs.LDAP.Core.LdapAttribute partialAttr = new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.Sequence);
                partialAttr.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.OctetString, AttributeName));
                global::Libs.LDAP.Core.LdapAttribute partialAttrVals = new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.Set);
                foreach (string AttributeValue in AttributeValues) { partialAttrVals.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.OctetString, AttributeValue)); }
                partialAttr.ChildAttributes.Add(partialAttrVals);
                partialAttributeList.ChildAttributes.Add(partialAttr);
            }
        }

        //'... Resolve this URGENTLY!
        private global::Libs.LDAP.Core.LdapPacket RespondUserData(global::Libs.LDAP.IUserData user, int MessageID, bool Simple)
        {
            global::Libs.LDAP.Core.LdapAttribute searchResultEntry = new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.LdapOperation.SearchResultEntry);
            searchResultEntry.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.OctetString, ("cn=" + user.UserName + "," + this._validator.LDAPRoot)));
            global::Libs.LDAP.Core.LdapAttribute partialAttributeList = new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.Sequence);
            //outlook request asked for options on the options section of the request (child[7]) as below | for some reason, sending more than 8 properties for outlook prevents it from completing the task (on outlook side)
            //this.AddAttribute(partialAttributeList, "cn", user.UserName);
            //this.AddAttribute(partialAttributeList, "commonName", user.UserName);
            //this.AddAttribute(partialAttributeList, "mail", user.EMail);
            if (!Simple)
            {
                //--- this.AddAttribute(partialAttributeList, "roleOccupant", "?"); //unused
            }
            //this.AddAttribute(partialAttributeList, "display-name", user.FullName);
            //this.AddAttribute(partialAttributeList, "displayname", user.FullName);
            if (!Simple)
            {
                //this.AddAttribute(partialAttributeList, "sn", user.LastName);
                //this.AddAttribute(partialAttributeList, "surname", user.LastName);
                //this.AddAttribute(partialAttributeList, "co", this._validator.Company.Country);
                //this.AddAttribute(partialAttributeList, "organizationName", this._validator.Company.Name);
                //--- this.AddAttribute(partialAttributeList, "o", "?"); //unused
                //this.AddAttribute(partialAttributeList, "givenName", user.FirstName);
                //--- this.AddAttribute(partialAttributeList, "legacyExcangeDN", "?"); //i have no need (don't know whats for)
                //this.AddAttribute(partialAttributeList, "objectClass", "?");
                //this.AddAttribute(partialAttributeList, "uid", "?");
                //this.AddAttribute(partialAttributeList, "mailNickname", user.FullName);
                //this.AddAttribute(partialAttributeList, "title", user.Job);
            }
            //this.AddAttribute(partialAttributeList, "company", this._validator.Company.Name);
            if (!Simple)
            {
                //--- this.AddAttribute(partialAttributeList, "physicalDeliveryOfficeName", "?"); //i have no need
                //this.AddAttribute(partialAttributeList, "telephoneNumber", this._validator.Company.Phone);
                //if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)) { this.AddAttribute(partialAttributeList, "initials", (user.FirstName.Substring(0, 1) + user.LastName.Substring(0, 1))); }
                //this.AddAttribute(partialAttributeList, "postalAddress", this._validator.Company.Address);
                //this.AddAttribute(partialAttributeList, "l", this._validator.Company.City);
                //this.AddAttribute(partialAttributeList, "st", this._validator.Company.State);
                //this.AddAttribute(partialAttributeList, "postalCode", this._validator.Company.PostCode);
                //this.AddAttribute(partialAttributeList, "ou", user.Department);
                //--- this.AddAttribute(partialAttributeList, "organizationUnitName", "?"); //unused
                //this.AddAttribute(partialAttributeList, "department", user.Department);
                //--- this.AddAttribute(partialAttributeList, "secretary", "?"); //i have no need
            }
            searchResultEntry.ChildAttributes.Add(partialAttributeList);
            global::Libs.LDAP.Core.LdapPacket response = new global::Libs.LDAP.Core.LdapPacket(MessageID);
            response.ChildAttributes.Add(searchResultEntry);
            return response;
        }

        private void WriteAttributes(byte[] pkB, global::System.Net.Sockets.NetworkStream stream) { stream.Write(pkB, 0, pkB.Length); }
        private void WriteAttributes(global::Libs.LDAP.Core.LdapAttribute attr, global::System.Net.Sockets.NetworkStream stream) { this.WriteAttributes(attr.GetBytes(), stream); }
        private void ReturnAllUsers(global::System.Net.Sockets.NetworkStream stream, int MessageID) { foreach (global::Libs.LDAP.IUserData user in this._validator.ListUsers()) { using (global::Libs.LDAP.Core.LdapPacket pkO = this.RespondUserData(user, MessageID, true)) { this.WriteAttributes(pkO, stream); } } }

        private void ReturnSingleUser(global::System.Net.Sockets.NetworkStream stream, int MessageID, string UserName)
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                UserName = UserName.ToLower();
                foreach (global::Libs.LDAP.IUserData user in this._validator.ListUsers()) { if (user.UserName == UserName) { using (global::Libs.LDAP.Core.LdapPacket pkO = this.RespondUserData(user, MessageID, false)) { this.WriteAttributes(pkO, stream); } break; } }
            }
        }

        private void ReturnTrue(global::System.Net.Sockets.NetworkStream stream, int MessageID)
        {
            global::Libs.LDAP.Core.LdapPacket pkO = new global::Libs.LDAP.Core.LdapPacket(MessageID);
            pkO.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.Boolean, true));
            pkO.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapAttribute(global::Libs.LDAP.Core.UniversalDataType.Sequence));
            byte[] pkB = pkO.GetBytes();
            pkO.Dispose();
            stream.Write(pkB, 0, pkB.Length);
        }

        //'... make better handling of the filters!
        private void HandleSearchRequest(global::System.Net.Sockets.NetworkStream stream, global::Libs.LDAP.Core.LdapPacket requestPacket, bool IsAdmin)
        {
            global::Libs.LDAP.Core.LdapAttribute searchRequest = global::Libs.LDAP.Core.Utils.SingleOrDefault<global::Libs.LDAP.Core.LdapAttribute>(requestPacket.ChildAttributes, o => { return o.LdapOperation == global::Libs.LDAP.Core.LdapOperation.SearchRequest; });
            global::Libs.LDAP.Core.LdapPacket responsePacket = new global::Libs.LDAP.Core.LdapPacket(requestPacket.MessageId);
            if (searchRequest == null) { responsePacket.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapResultAttribute(global::Libs.LDAP.Core.LdapOperation.SearchResultDone, global::Libs.LDAP.Core.LdapResult.compareFalse)); }
            else
            {
                string arg = searchRequest.ChildAttributes[0].GetValue<string>();
                if (arg != null && arg.Contains(this._validator.LDAPRoot))
                {
                    global::Libs.LDAP.Core.LdapAttribute filter = searchRequest.ChildAttributes[6];
                    global::Libs.LDAP.Core.LdapFilterChoice filterMode = (global::Libs.LDAP.Core.LdapFilterChoice)filter.ContextType;
                    arg = arg.Trim().Replace(this._validator.LDAPRoot, string.Empty).Trim();
                    if (arg.EndsWith(",")) { arg = arg.Substring(0, (arg.Length - 1)); }
                    if (arg.StartsWith("cn=")) { arg = arg.Substring(3); }
                    switch (filterMode)
                    {
                        case global::Libs.LDAP.Core.LdapFilterChoice.equalityMatch: this.ReturnSingleUser(stream, requestPacket.MessageId, arg); break;
                        case global::Libs.LDAP.Core.LdapFilterChoice.and:
                        case global::Libs.LDAP.Core.LdapFilterChoice.or: if (string.IsNullOrEmpty(arg) || this.IsValidType(arg)) { this.ReturnAllUsers(stream, requestPacket.MessageId); } else { this.ReturnSingleUser(stream, requestPacket.MessageId, arg); } break;
                        case global::Libs.LDAP.Core.LdapFilterChoice.present: this.ReturnSingleUser(stream, requestPacket.MessageId, arg); break;
                    }
                }
                responsePacket.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapResultAttribute(global::Libs.LDAP.Core.LdapOperation.SearchResultDone, global::Libs.LDAP.Core.LdapResult.success));
            }
            byte[] responseBytes = responsePacket.GetBytes();
            stream.Write(responseBytes, 0, responseBytes.Length);
        }

        private bool HandleBindRequest(global::System.IO.Stream stream, global::Libs.LDAP.Core.LdapPacket requestPacket, out bool IsAdmin)
        {
            IsAdmin = false;
            global::Libs.LDAP.Core.LdapAttribute bindrequest = global::Libs.LDAP.Core.Utils.SingleOrDefault<global::Libs.LDAP.Core.LdapAttribute>(requestPacket.ChildAttributes, o => { return o.LdapOperation == global::Libs.LDAP.Core.LdapOperation.BindRequest; });
            if (bindrequest == null) { return false; }
            else
            {
                string username = bindrequest.ChildAttributes[1].GetValue<string>();
                string password = bindrequest.ChildAttributes[2].GetValue<string>();
                global::Libs.LDAP.Core.LdapResult response = global::Libs.LDAP.Core.LdapResult.invalidCredentials;
                if (this._validator.Validate(username, password, out IsAdmin)) { response = global::Libs.LDAP.Core.LdapResult.success; }
                global::Libs.LDAP.Core.LdapPacket responsePacket = new global::Libs.LDAP.Core.LdapPacket(requestPacket.MessageId);
                responsePacket.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapResultAttribute(global::Libs.LDAP.Core.LdapOperation.BindResponse, response));
                byte[] responseBytes = responsePacket.GetBytes();
                stream.Write(responseBytes, 0, responseBytes.Length);
                return (response == global::Libs.LDAP.Core.LdapResult.success);
            }
        }

        private void OnClientConnect(global::System.IAsyncResult asyn) { this.HandleClient(this._server.EndAcceptTcpClient(asyn)); }

        private void HandleClient(global::System.Net.Sockets.TcpClient client)
        {
            this._server.BeginAcceptTcpClient(this.OnClientConnect, null);
            try
            {
                bool isBound = false;
                bool IsAdmin = false;
                bool nonSearch = true;
                global::System.Net.Sockets.NetworkStream stream = client.GetStream();
                global::Libs.LDAP.Core.LdapPacket requestPacket = null;
                while (global::Libs.LDAP.Core.LdapPacket.TryParsePacket(stream, out requestPacket))
                {
                    if (global::Libs.LDAP.Core.Utils.Any<global::Libs.LDAP.Core.LdapAttribute>(requestPacket.ChildAttributes, o => { return o.LdapOperation == global::Libs.LDAP.Core.LdapOperation.BindRequest; })) { isBound = this.HandleBindRequest(stream, requestPacket, out IsAdmin); }
                    if (isBound && global::Libs.LDAP.Core.Utils.Any<global::Libs.LDAP.Core.LdapAttribute>(requestPacket.ChildAttributes, o => { return o.LdapOperation == global::Libs.LDAP.Core.LdapOperation.SearchRequest; }))
                    {
                        nonSearch = false;
                        this.HandleSearchRequest(stream, requestPacket, IsAdmin);
                    }
                }
                if (nonSearch && (!isBound) && (requestPacket != null))
                {
                    global::Libs.LDAP.Core.LdapPacket responsePacket = new global::Libs.LDAP.Core.LdapPacket(requestPacket.MessageId);
                    responsePacket.ChildAttributes.Add(new global::Libs.LDAP.Core.LdapResultAttribute(global::Libs.LDAP.Core.LdapOperation.CompareResponse, global::Libs.LDAP.Core.LdapResult.compareFalse));
                    byte[] responseBytes = responsePacket.GetBytes();
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
            } catch { /* NOTHING */ }
        }

        internal static int Process(string[] args)
        {
            char Mode = default(char);
            if (args != null) { Mode = args[0].ToCharArray()[0]; }
            else
            {
                global::System.Console.WriteLine("Set Server [Y] or Client [N]. Set server first!");
                Mode = global::System.Console.ReadKey().KeyChar;
            }
            switch (Mode)
            {
                case 'y':
                case 'Y':
                    global::Libs.LDAP.Server s = new global::Libs.LDAP.Server(new global::Libs.LDAP.TestSource(), "127.0.0.1");
                    s.Start();
                    break;
                default:
#if CLIENT
                    global::Libs.LDAP.Root r = new global::Libs.LDAP.Root(global::Libs.LDAP.Root.GetRoot("user1", "1234", "127.0.0.1/cn=Users,dc=dev,dc=company,dc=com", QueryUser: false));
                    global::System.DirectoryServices.DirectoryEntry[] ms = global::Libs.LDAP.Root.GetChildren(r, FullTree: false, Filter: "(objectClass=posixAccount)");
                    global::System.Console.WriteLine(ms == null ? "0" : ms.Length.ToString());
#endif
                    break;
            }
            global::System.Console.ReadKey();
            return 0;
        }

#if CLIENT
        internal static int Main(string[] args) { return Process(new string[] { "n" }); }
#else
        internal static int Main(string[] args) { return Process(new string[] { "y" }); }
#endif

        protected Server(global::System.Net.IPEndPoint localEndpoint) { this._server = new global::System.Net.Sockets.TcpListener(localEndpoint); }
        public Server(global::Libs.LDAP.IDataSource Validator, global::System.Net.IPEndPoint localEndpoint) : this(localEndpoint) { this._validator = Validator; }
        public Server(global::Libs.LDAP.IDataSource Validator, string localEndpoint, int Port) : this(new global::System.Net.IPEndPoint(global::System.Net.IPAddress.Parse(localEndpoint), Port)) { this._validator = Validator; }
        public Server(global::Libs.LDAP.IDataSource Validator, string localEndpoint) : this(Validator, localEndpoint, global::Libs.LDAP.Server.StandardPort) { /* NOTHING */ }
    }
}
