using Sys = global::System;
using SysConv = global::System.Convert;
using SysTxt = global::System.Text;
using SysCll = global::System.Collections;
using SysClG = global::System.Collections.Generic;
using SysSock = global::System.Net.Sockets;
using LDap = global::Libs.LDAP;
using LCore = global::Libs.LDAP.Core;
namespace Libs.LDAP //https://docs.iredmail.org/use.openldap.as.address.book.in.outlook.html
{
    namespace Core //https://github.com/vforteli/Flexinets.Ldap.Core | https://tools.ietf.org/html/rfc4511
    {
        internal delegate bool Verify<T>(T obj);

        internal static class Utils
        {
            internal static byte[] StringToByteArray(string hex, bool trimWhitespace = true)
            {
                if (trimWhitespace) { hex = hex.Replace(" ", string.Empty); }
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2) { bytes[i / 2] = SysConv.ToByte(hex.Substring(i, 2), 16); }
                return bytes;
            }

            internal static string ByteArrayToString(byte[] bytes)
            {
                SysTxt.StringBuilder hex = new SysTxt.StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) { hex.Append(b.ToString("X2")); }
                return hex.ToString();
            }

            internal static string BitsToString(SysCll.BitArray bits)
            {
                int i = 1;
                string derp = string.Empty;
                foreach (object bit in bits)
                {
                    derp += SysConv.ToInt32(bit);
                    if (i % 8 == 0) { derp += " "; }
                    i++;
                }
                return derp.Trim();
            }

            internal static byte[] IntToBerLength(int length) //https://en.wikipedia.org/wiki/X.690#BER_encoding
            {
                if (length <= 127) { return new byte[] { (byte)length }; }
                else
                {
                    byte[] intbytes = Sys.BitConverter.GetBytes(length);
                    Sys.Array.Reverse(intbytes);
                    byte intbyteslength = (byte)intbytes.Length;
                    int lengthByte = intbyteslength + 128;
                    byte[] berBytes = new byte[1 + intbyteslength];
                    berBytes[0] = (byte)lengthByte;
                    Sys.Buffer.BlockCopy(intbytes, 0, berBytes, 1, intbyteslength);
                    return berBytes;
                }
            }

            internal static TObject[] Reverse<TObject>(SysClG.IEnumerable<TObject> enumerable)
            {
                SysClG.List<TObject> acum = new SysClG.List<TObject>(10);
                foreach (TObject obj in enumerable)
                {
                    if (acum.Count == acum.Capacity) { acum.Capacity += 10; }
                    acum.Add(obj);
                }
                acum.Reverse();
                return acum.ToArray();
            }

            internal static bool Any<T>(SysClG.IEnumerable<T> enumerator, LCore.Verify<T> verifier) { foreach (T obj in enumerator) { if (verifier(obj)) { return true; } } return false; }
            internal static T SingleOrDefault<T>(SysClG.IEnumerable<T> enumerator, LCore.Verify<T> verifier) { foreach (T obj in enumerator) { if (verifier(obj)) { return obj; } } return default(T); }

            private sealed class ArraySegmentEnumerator<T> : SysClG.IEnumerator<T>, SysClG.IEnumerable<T> //https://referencesource.microsoft.com/#mscorlib/system/arraysegment.cs,9b6becbc5eb6a533
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

                public T Current { get { if (this._current < this._start) throw new Sys.InvalidOperationException(); else if (this._current >= this._end) throw new Sys.InvalidOperationException(); else return this._array[this._current]; } }
                SysClG.IEnumerator<T> SysClG.IEnumerable<T>.GetEnumerator() { return this; }
                SysCll.IEnumerator SysCll.IEnumerable.GetEnumerator() { return this; }
                object SysCll.IEnumerator.Current { get { return this.Current; } }
                void SysCll.IEnumerator.Reset() { this._current = this._start - 1; }
                void Sys.IDisposable.Dispose() { /* NOTHING */ }

                internal ArraySegmentEnumerator(T[] Array, int Start, int Count)
                {
                    this._array = Array;
                    this._start = Start;
                    this._end = this._start + Count;
                    this._current = this._start - 1;
                }
            }

            internal static int BerLengthToInt(byte[] bytes, int offset, out int berByteCount)
            {
                berByteCount = 1;
                int attributeLength = 0;
                if (bytes[offset] >> 7 == 1)
                {
                    int lengthoflengthbytes = bytes[offset] & 127;
                    byte[] temp = LCore.Utils.Reverse<byte>(new LCore.Utils.ArraySegmentEnumerator<byte>(bytes, offset + 1, lengthoflengthbytes));
                    Sys.Array.Resize<byte>(ref temp, 4);
                    attributeLength = Sys.BitConverter.ToInt32(temp, 0);
                    berByteCount += lengthoflengthbytes;
                } else { attributeLength = bytes[offset] & 127; }
                return attributeLength;
            }

            internal static int BerLengthToInt(Sys.IO.Stream stream, out int berByteCount)
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
                    byte[] temp = LCore.Utils.Reverse<byte>(lengthBytes);
                    Sys.Array.Resize<byte>(ref temp, 4);
                    attributeLength = Sys.BitConverter.ToInt32(temp, 0);
                    berByteCount += lengthoflengthbytes;
                } else { attributeLength = berByte[0] & 127; }
                return attributeLength;
            }

            internal static string Repeat(string stuff, int n)
            {
                SysTxt.StringBuilder concat = new SysTxt.StringBuilder(stuff.Length * n);
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

        public enum LdapResult : byte
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

        public enum UniversalDataType : byte
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
            public LCore.TagClass Class { get { return (LCore.TagClass)(this.TagByte >> 6); } }
            public LCore.UniversalDataType DataType { get { return this.Class == LCore.TagClass.Universal ? (LCore.UniversalDataType)(this.TagByte & 31) : LCore.UniversalDataType.NONE; } }
            public LCore.LdapOperation LdapOperation { get { return this.Class == LCore.TagClass.Application ? (LCore.LdapOperation)(this.TagByte & 31) : LCore.LdapOperation.NONE; } }
            public byte? ContextType { get { return this.Class == LCore.TagClass.Context ? (byte?)(this.TagByte & 31) : null; } }
            public static LCore.Tag Parse(byte tagByte) { return new LCore.Tag { TagByte = tagByte }; }
            public override string ToString() { return "Tag[class=" + this.Class.ToString() + ",datatype=" + this.DataType.ToString() + ",ldapoperation=" + this.LdapOperation.ToString() + ",contexttype=" + (this.ContextType == null ? "NULL" : ((LCore.LdapFilterChoice)this.ContextType).ToString()) + "]"; }

            public bool IsConstructed
            {
                get { return new SysCll.BitArray(new byte[] { this.TagByte }).Get(5); }
                set
                {
                    SysCll.BitArray foo = new SysCll.BitArray(new byte[] { this.TagByte });
                    foo.Set(5, value);
                    byte[] temp = new byte[1];
                    foo.CopyTo(temp, 0);
                    this.TagByte = temp[0];
                }
            }

            private Tag() { /* NOTHING */ }
            public Tag(LCore.LdapOperation operation) { TagByte = (byte)((byte)operation + ((byte)LCore.TagClass.Application << 6)); }
            public Tag(LCore.UniversalDataType dataType) { TagByte = (byte)(dataType + ((byte)LCore.TagClass.Universal << 6)); }
            public Tag(byte context) { TagByte = (byte)(context + ((byte)LCore.TagClass.Context << 6)); }
        }

        public class LdapAttribute : Sys.IDisposable
        {
            private LCore.Tag _tag;
            protected byte[] Value = new byte[0];
            public SysClG.List<LCore.LdapAttribute> ChildAttributes = new SysClG.List<LCore.LdapAttribute>();
            public LCore.TagClass Class { get { return this._tag.Class; } }
            public bool IsConstructed { get { return (this._tag.IsConstructed || this.ChildAttributes.Count > 0); } }
            public LCore.LdapOperation LdapOperation { get { return this._tag.LdapOperation; } }
            public LCore.UniversalDataType DataType { get { return this._tag.DataType; } }
            public byte? ContextType { get { return this._tag.ContextType; } }

            public object GetValue()
            {
                if (this._tag.Class == LCore.TagClass.Universal)
                {
                    switch (this._tag.DataType)
                    {
                        case LCore.UniversalDataType.Boolean: return Sys.BitConverter.ToBoolean(this.Value, 0);
                        case LCore.UniversalDataType.Integer:
                            byte[] intbytes = new byte[4];
                            Sys.Buffer.BlockCopy(this.Value, 0, intbytes, 4 - this.Value.Length, this.Value.Length);
                            Sys.Array.Reverse(intbytes);
                            return Sys.BitConverter.ToInt32(intbytes, 0);
                        default: return SysTxt.Encoding.UTF8.GetString(this.Value, 0, this.Value.Length);
                    }
                }
                return SysTxt.Encoding.UTF8.GetString(Value, 0, Value.Length);
            }

            private byte[] GetBytes(object val)
            {
                if (val == null) { return new byte[0]; }
                else
                {
                    Sys.Type typeOFval = val.GetType();
                    if (typeOFval == typeof(string)) { return SysTxt.Encoding.UTF8.GetBytes(val as string); }
                    else if (typeOFval == typeof(int)) { return LCore.Utils.Reverse<byte>(Sys.BitConverter.GetBytes((int)val)); }
                    else if (typeOFval == typeof(byte)) { return new byte[] { (byte)val }; }
                    else if (typeOFval == typeof(bool)) { return Sys.BitConverter.GetBytes((bool)val); }
                    else if (typeOFval == typeof(byte[])) { return (val as byte[]); }
                    else { throw new Sys.InvalidOperationException("Nothing found for " + typeOFval); }
                }
            }

            public override string ToString() { return this._tag.ToString() + ",Value={" + ((this.Value == null || this.Value.Length == 0) ? "\"\"" : SysTxt.Encoding.UTF8.GetString(this.Value)) + "},attr=" + this.ChildAttributes.Count.ToString(); }
            public T GetValue<T>() { return (T)SysConv.ChangeType(this.GetValue(), typeof(T)); }

            public byte[] GetBytes()
            {
                SysClG.List<byte> contentBytes = new SysClG.List<byte>();
                if (ChildAttributes.Count > 0)
                {
                    this._tag.IsConstructed = true;
                    foreach (LCore.LdapAttribute attr in this.ChildAttributes) { contentBytes.AddRange(attr.GetBytes()); }
                } else { contentBytes.AddRange(Value); }
                SysClG.List<byte> ret = new System.Collections.Generic.List<byte>(1);
                ret.Add(this._tag.TagByte);
                ret.AddRange(LCore.Utils.IntToBerLength(contentBytes.Count));
                ret.Capacity += contentBytes.Count;
                ret.AddRange(contentBytes);
                contentBytes.Clear();
                contentBytes = null;
                return ret.ToArray();
            }

            public virtual void Dispose()
            {
                this.Value = null;
                foreach (LCore.LdapAttribute attr in this.ChildAttributes) { attr.Dispose(); }
                this.ChildAttributes.Clear();
            }

            protected static SysClG.List<LCore.LdapAttribute> ParseAttributes(byte[] bytes, int currentPosition, int length)
            {
                SysClG.List<LCore.LdapAttribute> list = new SysClG.List<LCore.LdapAttribute>();
                while (currentPosition < length)
                {
                    LCore.Tag tag = LCore.Tag.Parse(bytes[currentPosition]);
                    currentPosition++;
                    int i = 0;
                    int attributeLength = LCore.Utils.BerLengthToInt(bytes, currentPosition, out i);
                    currentPosition += i;
                    LCore.LdapAttribute attribute = new LCore.LdapAttribute(tag);
                    if (tag.IsConstructed && attributeLength > 0) { attribute.ChildAttributes = ParseAttributes(bytes, currentPosition, currentPosition + attributeLength); }
                    else
                    {
                        attribute.Value = new byte[attributeLength];
                        Sys.Buffer.BlockCopy(bytes, currentPosition, attribute.Value, 0, attributeLength);
                    }
                    list.Add(attribute);
                    currentPosition += attributeLength;
                }
                return list;
            }

            protected LdapAttribute(LCore.Tag tag) { this._tag = tag; }
            public LdapAttribute(LCore.LdapOperation operation) { this._tag = new LCore.Tag(operation); }
            public LdapAttribute(LCore.LdapOperation operation, object value) : this(operation) { this.Value = this.GetBytes(value); }
            public LdapAttribute(LCore.UniversalDataType dataType) { this._tag = new LCore.Tag(dataType); }
            public LdapAttribute(LCore.UniversalDataType dataType, object value) : this(dataType) { this.Value = this.GetBytes(value); }
            public LdapAttribute(byte contextType) { this._tag = new LCore.Tag(contextType); }
            public LdapAttribute(byte contextType, object value) : this(contextType) { this.Value = this.GetBytes(value); }
        }

        public class LdapResultAttribute : LCore.LdapAttribute
        {
            public LdapResultAttribute(LCore.LdapOperation operation, LCore.LdapResult result, string matchedDN = "", string diagnosticMessage = "") : base(operation)
            {
                this.ChildAttributes.Add(new LCore.LdapAttribute(LCore.UniversalDataType.Enumerated, (byte)result));
                this.ChildAttributes.Add(string.IsNullOrEmpty(matchedDN) ? new LCore.LdapAttribute(LCore.UniversalDataType.OctetString, false) : new LCore.LdapAttribute(LCore.UniversalDataType.OctetString, matchedDN));
                this.ChildAttributes.Add(string.IsNullOrEmpty(diagnosticMessage) ? new LCore.LdapAttribute(LCore.UniversalDataType.OctetString, false) : new LCore.LdapAttribute(LCore.UniversalDataType.OctetString, diagnosticMessage));
            }
        }

        public class LdapPacket : LCore.LdapAttribute
        {
            public int MessageId { get { return this.ChildAttributes[0].GetValue<int>(); } }

            public static LCore.LdapPacket ParsePacket(byte[] bytes)
            {
                LCore.LdapPacket packet = new LCore.LdapPacket(LCore.Tag.Parse(bytes[0]));
                int lengthBytesCount = 0;
                int contentLength = LCore.Utils.BerLengthToInt(bytes, 1, out lengthBytesCount);
                packet.ChildAttributes.AddRange(LCore.LdapAttribute.ParseAttributes(bytes, 1 + lengthBytesCount, contentLength));
                return packet;
            }

            public static bool TryParsePacket(Sys.IO.Stream stream, out LCore.LdapPacket packet)
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
                            int contentLength = LCore.Utils.BerLengthToInt(stream, out n);
                            byte[] contentBytes = new byte[contentLength];
                            stream.Read(contentBytes, 0, contentLength);
                            packet = new LCore.LdapPacket(LCore.Tag.Parse(tagByte[0]));
                            packet.ChildAttributes.AddRange(LCore.LdapAttribute.ParseAttributes(contentBytes, 0, contentLength));
                            return true;
                        }
                    }
                } catch { /* NOTHING */ }
                packet = null;
                return false;
            }

            private LdapPacket(LCore.Tag tag) : base(tag) { /* NOTHING */ }
            public LdapPacket(int messageId) : base(LCore.UniversalDataType.Sequence) { this.ChildAttributes.Add(new LCore.LdapAttribute(LCore.UniversalDataType.Integer, messageId)); }
        }
    }

    internal struct SearchKey
    {
        internal string Key;
        internal string[] Values;

        internal SearchKey(string Key, string[] Values) : this()
        {
            this.Key = Key;
            this.Values = Values;
        }

        internal SearchKey(string Key, string Value) : this(Key, new string[] { Value }) { /* NOTHING */ }
    }

    internal struct SearchValue
    {
        internal string[] Keys;
        internal string Value;

        internal SearchValue(string[] Keys, string Value) : this()
        {
            this.Keys = Keys;
            this.Value = Value;
        }

        internal SearchValue(string Key, string Value) : this(new string[] { Key }, Value) { /* NOTHING */ }
    }

    internal class SearchCondition
    {
        internal LCore.LdapFilterChoice Filter = LCore.LdapFilterChoice.or;
        internal SysClG.List<LDap.SearchKey> Keys = new SysClG.List<LDap.SearchKey>(30);
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
        string Mobile { get; }
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
        LDap.ICompany Company { get; }
        bool Validate(string UserName, string Password, out bool IsAdmin);
        SysClG.IEnumerable<LDap.IUserData> ListUsers();
    }

    public class Server //https://github.com/vforteli/Flexinets.Ldap.Server/blob/master/LdapServer.cs
    {
        public const int StandardPort = 389;
        public const string AMAccount = "sAMAccountName";
        private readonly SysSock.TcpListener _server;
        private LDap.IDataSource _validator;
        public LDap.IDataSource Validator { get { return this._validator; } }
        private bool IsValidType(string type) { return (type == "objectClass" || type == LDap.Server.AMAccount); }
        public void Stop() { if (this._server != null) { this._server.Stop(); } }
        private LDap.SearchValue GetCompare(LDap.SearchValue[] pack, LDap.SearchKey key) { foreach (LDap.SearchValue val in pack) { foreach (string valKey in val.Keys) { if (valKey == key.Key) { return val; } } } return default(LDap.SearchValue); }
        private bool IsBind(LCore.LdapAttribute attr) { return (attr.LdapOperation == LCore.LdapOperation.BindRequest); }
        private bool IsSearch(LCore.LdapAttribute attr) { return (attr.LdapOperation == LCore.LdapOperation.SearchRequest); }
        private void WriteAttributes(byte[] pkB, SysSock.NetworkStream stream) { stream.Write(pkB, 0, pkB.Length); }
        private void WriteAttributes(LCore.LdapAttribute attr, SysSock.NetworkStream stream) { this.WriteAttributes(attr.GetBytes(), stream); }

        private int Matched(LDap.SearchValue[] pack, LDap.SearchKey key)
        {
            LDap.SearchValue comp = this.GetCompare(pack, key);
            if (comp.Keys == null || comp.Keys.Length == 0) { return -1; }
            else if (key.Values == null || key.Values.Length == 0 || (key.Values.Length == 1 && (key.Values[0] == "*" || key.Values[0] == ""))) { return 2; }
            else
            {
                int m = 0;
                foreach (string kv in key.Values) { if (comp.Value != null && comp.Value.IndexOf(kv, 0, Sys.StringComparison.CurrentCultureIgnoreCase) > -1) { m++; break; } }
                if (m == key.Values.Length) { m = 2; } else if (m > 0) { m = 1; }
                return m;
            }
        }

        public void Start()
        {
            this._server.Start();
            this._server.BeginAcceptTcpClient(this.OnClientConnect, null);
        }

        private void AddAttribute(LCore.LdapAttribute partialAttributeList, string AttributeName, string AttributeValue)
        {
            if (!string.IsNullOrEmpty(AttributeValue))
            {
                LCore.LdapAttribute partialAttr = new LCore.LdapAttribute(LCore.UniversalDataType.Sequence);
                partialAttr.ChildAttributes.Add(new LCore.LdapAttribute(LCore.UniversalDataType.OctetString, AttributeName));
                LCore.LdapAttribute partialAttrVals = new LCore.LdapAttribute(LCore.UniversalDataType.Set);
                partialAttrVals.ChildAttributes.Add(new LCore.LdapAttribute(LCore.UniversalDataType.OctetString, AttributeValue));
                partialAttr.ChildAttributes.Add(partialAttrVals);
                partialAttributeList.ChildAttributes.Add(partialAttr);
            }
        }

        private LDap.SearchValue[] UserPack(LDap.IUserData user)
        {
            LDap.SearchValue[] pk = new LDap.SearchValue[17];
            pk[0] = new LDap.SearchValue(new string[] { "cn", "commonName" }, user.UserName);
            pk[1] = new LDap.SearchValue(new string[] { "mail" }, user.EMail);
            pk[2] = new LDap.SearchValue(new string[] { "displayname", "display-name", "mailNickname", "mozillaNickname" }, user.FullName);
            pk[3] = new LDap.SearchValue(new string[] { "givenName" }, user.FirstName);
            pk[4] = new LDap.SearchValue(new string[] { "sn", "surname" }, user.LastName);
            pk[5] = new LDap.SearchValue(new string[] { "ou", "department" }, user.Department);
            pk[6] = new LDap.SearchValue(new string[] { "co", "countryname" }, this._validator.Company.Country);
            pk[7] = new LDap.SearchValue(new string[] { "postalAddress", "streetaddress" }, this._validator.Company.Address);
            pk[8] = new LDap.SearchValue(new string[] { "company", "organizationName" }, this._validator.Company.Name);
            pk[9] = new LDap.SearchValue(new string[] { "objectClass" }, LDap.Server.AMAccount);
            pk[10] = new LDap.SearchValue(new string[] { "title" }, user.Job);
            pk[11] = new LDap.SearchValue(new string[] { "telephoneNumber" }, this._validator.Company.Phone);
            pk[12] = new LDap.SearchValue(new string[] { "l" }, this._validator.Company.City);
            pk[13] = new LDap.SearchValue(new string[] { "st" }, this._validator.Company.State);
            pk[14] = new LDap.SearchValue(new string[] { "postalCode" }, this._validator.Company.PostCode);
            pk[15] = new LDap.SearchValue(new string[] { "mobile" }, user.Mobile);
            pk[16] = new LDap.SearchValue(new string[] { "initials" }, ((!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)) ? (user.FirstName.Substring(0, 1) + user.LastName.Substring(0, 1)) : string.Empty));
            //--- ("uid", "r1"); //unused
            //--- ("o", "r2"); //unused
            //--- ("legacyExcangeDN", "r3"); //i have no need (don't know whats for)
            //--- ("physicalDeliveryOfficeName", "r4"); //i have no need
            //--- ("secretary", "r5"); //i have no need
            //--- ("roleOccupant", "r6"); //unused
            //--- ("organizationUnitName", "r7"); //unused
            return pk;
        }

        private LCore.LdapPacket RespondUserData(LDap.IUserData user, LDap.SearchValue[] pack, int MessageID)
        {
            LCore.LdapAttribute searchResultEntry = new LCore.LdapAttribute(LCore.LdapOperation.SearchResultEntry);
            searchResultEntry.ChildAttributes.Add(new LCore.LdapAttribute(LCore.UniversalDataType.OctetString, ("cn=" + user.UserName + "," + this._validator.LDAPRoot)));
            LCore.LdapAttribute partialAttributeList = new LCore.LdapAttribute(LCore.UniversalDataType.Sequence);
            foreach (LDap.SearchValue pkItem in pack) { foreach (string Key in pkItem.Keys) { this.AddAttribute(partialAttributeList, Key, pkItem.Value); } }
            searchResultEntry.ChildAttributes.Add(partialAttributeList);
            LCore.LdapPacket response = new LCore.LdapPacket(MessageID);
            response.ChildAttributes.Add(searchResultEntry);
            return response;
        }

        private void ReturnAllUsers(SysSock.NetworkStream stream, int MessageID, int Limit)
        {
            foreach (LDap.IUserData user in this._validator.ListUsers())
            {
                if (Limit > 0)
                {
                    using (LCore.LdapPacket pkO = this.RespondUserData(user, this.UserPack(user), MessageID)) { this.WriteAttributes(pkO, stream); }
                    Limit--;
                } else { break; }
            }
        }

        private void ReturnSingleUser(SysSock.NetworkStream stream, int MessageID, string UserName)
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                UserName = UserName.ToLower();
                foreach (LDap.IUserData user in this._validator.ListUsers()) { if (user.UserName == UserName) { using (LCore.LdapPacket pkO = this.RespondUserData(user, this.UserPack(user), MessageID)) { this.WriteAttributes(pkO, stream); } break; } }
            }
        }

        private void ReturnTrue(SysSock.NetworkStream stream, int MessageID)
        {
            LCore.LdapPacket pkO = new LCore.LdapPacket(MessageID);
            pkO.ChildAttributes.Add(new LCore.LdapAttribute(LCore.UniversalDataType.Boolean, true));
            pkO.ChildAttributes.Add(new LCore.LdapAttribute(LCore.UniversalDataType.Sequence));
            this.WriteAttributes(pkO, stream);
        }

        private string ExtractUser(string arg)
        {
            if (!string.IsNullOrEmpty(arg))
            {
                arg = arg.Trim().Replace(this._validator.LDAPRoot, string.Empty).Trim();
                if (arg.EndsWith(",")) { arg = arg.Substring(0, (arg.Length - 1)); }
                if (arg.StartsWith("cn=")) { arg = arg.Substring(3); }
            }
            return arg;
        }

        private LDap.SearchCondition GetSearchOptions(LCore.LdapAttribute filter)
        {
            LDap.SearchKey cur = new LDap.SearchKey("*", filter.GetValue<string>());
            LDap.SearchCondition args = new LDap.SearchCondition();
            try
            {
                args.Filter = (LCore.LdapFilterChoice)filter.ContextType;
                if (string.IsNullOrEmpty(cur.Values[0]))
                {
                    if (filter.ChildAttributes.Count == 1) { filter = filter.ChildAttributes[0]; }
                    if (filter.ChildAttributes.Count > 0)
                    {
                        args.Filter = (LCore.LdapFilterChoice)filter.ContextType;
                        string[] nARG = null;
                        LCore.LdapAttribute varg = null;
                        foreach (LCore.LdapAttribute arg in filter.ChildAttributes)
                        {
                            if (arg.ChildAttributes.Count == 2 && arg.ChildAttributes[0].DataType == LCore.UniversalDataType.OctetString)
                            {
                                cur = new LDap.SearchKey(arg.ChildAttributes[0].GetValue<string>(), (null as string[]));
                                varg = arg.ChildAttributes[1];
                                if (varg.DataType == LCore.UniversalDataType.OctetString) { cur.Values = new string[] { varg.GetValue<string>() }; }
                                else
                                {
                                    nARG = new string[varg.ChildAttributes.Count];
                                    for (int i = 0; i < varg.ChildAttributes.Count; i++) { nARG[i] = varg.ChildAttributes[i].GetValue<string>(); }
                                    cur.Values = nARG;
                                    nARG = null;
                                }
                                if (!string.IsNullOrEmpty(cur.Key)) { args.Keys.Add(cur); }
                            }
                        }
                    }
                } else { args.Keys.Add(cur); }
            } catch { args.Keys.Clear(); }
            return args;
        }

        private void ReturnUsers(SysSock.NetworkStream stream, int MessageID, int Limit, LDap.SearchCondition args)
        {
            LDap.SearchValue[] pack = null;
            bool Matched = false;
            int mcount = -1;
            foreach (LDap.IUserData user in this._validator.ListUsers())
            {
                Matched = false;
                if (Limit > 0)
                {
                    pack = this.UserPack(user);
                    switch (args.Filter)
                    {
                        case LCore.LdapFilterChoice.or:
                            foreach (LDap.SearchKey key in args.Keys)
                            {
                                mcount = this.Matched(pack, key);
                                if (mcount > 0) { Matched = true; break; }
                            }
                            break;
                        case LCore.LdapFilterChoice.and:
                            if (args.Keys.Count == pack.Length) //Since all must match anyway
                            {
                                Matched = true;
                                foreach (LDap.SearchKey key in args.Keys)
                                {
                                    mcount = this.Matched(pack, key);
                                    if (mcount != 2) { Matched = false; break; }
                                }
                            }
                            break;
                    }
                    if (Matched)
                    {
                        using (LCore.LdapPacket pkO = this.RespondUserData(user, pack, MessageID)) { this.WriteAttributes(pkO, stream); }
                        Limit--;
                    }
                } else { break; }
            }
        }

        private void HandleSearchRequest(SysSock.NetworkStream stream, LCore.LdapPacket requestPacket, bool IsAdmin)
        {
            LCore.LdapAttribute searchRequest = LCore.Utils.SingleOrDefault<LCore.LdapAttribute>(requestPacket.ChildAttributes, o => { return o.LdapOperation == LCore.LdapOperation.SearchRequest; });
            LCore.LdapPacket responsePacket = new LCore.LdapPacket(requestPacket.MessageId);
            if (searchRequest == null) { responsePacket.ChildAttributes.Add(new LCore.LdapResultAttribute(LCore.LdapOperation.SearchResultDone, LCore.LdapResult.compareFalse)); }
            else
            {
                int limit = searchRequest.ChildAttributes[3].GetValue<int>();
                if (limit == 0) { limit = 999; } //max on outlook | target client
                string arg = searchRequest.ChildAttributes[0].GetValue<string>();
                LCore.LdapAttribute filter = searchRequest.ChildAttributes[6];
                LCore.LdapFilterChoice filterMode = (LCore.LdapFilterChoice)filter.ContextType;
                if (arg != null && arg.Contains(this._validator.LDAPRoot))
                {
                    arg = this.ExtractUser(arg);
                    switch (filterMode)
                    {
                        case LCore.LdapFilterChoice.equalityMatch:
                        case LCore.LdapFilterChoice.present: this.ReturnSingleUser(stream, requestPacket.MessageId, arg); break;
                        case LCore.LdapFilterChoice.and:
                        case LCore.LdapFilterChoice.or:
                            if (string.IsNullOrEmpty(arg) || this.IsValidType(arg))
                            {
                                LDap.SearchCondition args = this.GetSearchOptions(filter);
                                if (args.Keys.Count == 0 || args.Keys[0].Key == "*") { this.ReturnAllUsers(stream, requestPacket.MessageId, limit); } else { this.ReturnUsers(stream, requestPacket.MessageId, limit, args); }
                            } else { this.ReturnSingleUser(stream, requestPacket.MessageId, arg); }
                            break;
                    }
                }
                else
                {
                    arg = filter.GetValue<string>();
                    if (!string.IsNullOrEmpty(arg))
                    {
                        switch (filterMode)
                        {
                            case LCore.LdapFilterChoice.present: if (this.IsValidType(arg)) { this.ReturnTrue(stream, requestPacket.MessageId); } break;
                            default: break; //NOTHING YET!
                        }
                    }
                }
                responsePacket.ChildAttributes.Add(new LCore.LdapResultAttribute(LCore.LdapOperation.SearchResultDone, LCore.LdapResult.success));
            }
            this.WriteAttributes(responsePacket, stream);
        }

        private bool HandleBindRequest(SysSock.NetworkStream stream, LCore.LdapPacket requestPacket, out bool IsAdmin)
        {
            IsAdmin = false;
            LCore.LdapAttribute bindrequest = LCore.Utils.SingleOrDefault<LCore.LdapAttribute>(requestPacket.ChildAttributes, o => { return o.LdapOperation == LCore.LdapOperation.BindRequest; });
            if (bindrequest == null) { return false; }
            else
            {
                string username = this.ExtractUser(bindrequest.ChildAttributes[1].GetValue<string>());
                string password = bindrequest.ChildAttributes[2].GetValue<string>();
                LCore.LdapResult response = LCore.LdapResult.invalidCredentials;
                if (this._validator.Validate(username, password, out IsAdmin)) { response = LCore.LdapResult.success; }
                LCore.LdapPacket responsePacket = new LCore.LdapPacket(requestPacket.MessageId);
                responsePacket.ChildAttributes.Add(new LCore.LdapResultAttribute(LCore.LdapOperation.BindResponse, response));
                this.WriteAttributes(responsePacket, stream);
                return (response == LCore.LdapResult.success);
            }
        }

        private void HandleClient(SysSock.TcpClient client)
        {
            this._server.BeginAcceptTcpClient(this.OnClientConnect, null);
            try
            {
                bool isBound = false;
                bool IsAdmin = false;
                bool nonSearch = true;
                SysSock.NetworkStream stream = client.GetStream();
                LCore.LdapPacket requestPacket = null;
                while (LCore.LdapPacket.TryParsePacket(stream, out requestPacket))
                {
                    if (LCore.Utils.Any<LCore.LdapAttribute>(requestPacket.ChildAttributes, this.IsBind)) { isBound = this.HandleBindRequest(stream, requestPacket, out IsAdmin); }
                    if (isBound && LCore.Utils.Any<LCore.LdapAttribute>(requestPacket.ChildAttributes, this.IsSearch))
                    {
                        nonSearch = false;
                        this.HandleSearchRequest(stream, requestPacket, IsAdmin);
                    }
                }
                if (nonSearch && (!isBound) && (requestPacket != null))
                {
                    LCore.LdapPacket responsePacket = new LCore.LdapPacket(requestPacket.MessageId);
                    responsePacket.ChildAttributes.Add(new LCore.LdapResultAttribute(LCore.LdapOperation.CompareResponse, LCore.LdapResult.compareFalse));
                    this.WriteAttributes(responsePacket, stream);
                }
#if TESTING
            } catch (Sys.Exception e) { Sys.Console.WriteLine(Sys.DateTime.Now.ToString() + " exception: " + e.Message); }
#else
            } catch { /* NOTHING */ }
#endif
        }

        private void OnClientConnect(Sys.IAsyncResult asyn) { this.HandleClient(this._server.EndAcceptTcpClient(asyn)); }
        protected Server(Sys.Net.IPEndPoint localEndpoint) { this._server = new SysSock.TcpListener(localEndpoint); }
        public Server(LDap.IDataSource Validator, Sys.Net.IPEndPoint localEndpoint) : this(localEndpoint) { this._validator = Validator; }
        public Server(LDap.IDataSource Validator, string localEndpoint, int Port) : this(new Sys.Net.IPEndPoint(Sys.Net.IPAddress.Parse(localEndpoint), Port)) { this._validator = Validator; }
        public Server(LDap.IDataSource Validator, string localEndpoint) : this(Validator, localEndpoint, LDap.Server.StandardPort) { /* NOTHING */ }
    }
#if TESTING
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
        public string LDAPRoot { get { return "cn=Users,dc=company,dc=com"; } }
        public string AdminUser { get { return "admin"; } }
        public string AdminPassword { get { return "12345"; } }

        public SysClG.IEnumerable<LDap.IUserData> ListUsers()
        {
            yield return new LDap.UserData("ainz.ooal.gown", "ainzsama@nazarick.com", "Ainz", "Ooal Gown") { Department = "Nazarick Mausoleum", Job = "Overlord", Mobile = "+9900900000099" };
            yield return new LDap.UserData("shalltear.bff", "shalltear@nazarick.com", "Shalltear", "Bloodfallen") { Department = "Base Floors", Job = "Guardian" };
            yield return new LDap.UserData("narberal", "narberal@nazarick.com", "Narberal", "Gamma") { Department = "Floor 10", Job = "Pleiade" };
            yield return new LDap.UserData("sebas.tian", "sebas@nazarick.com", "Sebas", "Tian") { Department = "Floor 10", Job = "Buttler" };
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

        internal static int Main(string[] args)
        {
            LDap.Server s = new LDap.Server(new LDap.TestSource(), "127.0.0.1");
            s.Start();
            Sys.Console.ReadKey(); //without this the program would end; the server must be hosted on a persisting task
            return 0;
        }

        public TestSource() { this.Company = new LDap.Company() { Name = "Nazarick Inc.", Phone = "+9900900000000", Country = "Baharuth", State = "E-Rantel", City = "Nazarick", PostCode = "12123123", Address = "An Adress of" }; }
    }
#endif
}
