using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegistryClass
{
    public class RegObject
    {
        public string KeyName { get; set; }
        public string SubKey { get; set; }
        public object Value { get; set; }
        public RegistryHelper.VALUE_TYPE Type;
        public RegistryHelper.ROOT_KEY RootKey;

        public override string ToString()
        {
            string result = "";
            List<string> slList = this.ToStringList();
            if (slList == null) return null;

            foreach (string str in slList)
            {
                if (!result.Equals(""))
                {
                    result += "\0";
                }

                result += str;
            }

            return result;
        }

        public long ToLong()
        {
            if (this.Value == null)
            {
                return -1;
            }

            try
            {
                return Convert.ToInt64(this.Value);
            }
            catch
            {
                return -1;
            }
        }

        public string[] ToStringArray()
        {
            if (this.Value == null)
            {
                return null;
            }

            if (this.Value.GetType() == typeof(string[]))
            {
                return (string[])this.Value;
            }

            if (this.Value.GetType() == typeof(byte[]))
            {
                return new string[] { new RegistryHelper().ByteArrayToString((byte[])this.Value) };
            }

            return new string[] { Convert.ToString(this.Value) };
        }

        public List<string> ToStringList()
        {
            string[] arr = this.ToStringArray();
            List<string> resultList = new List<string>();
            if (arr == null) return null;
            resultList.InsertRange(0, arr);

            return resultList;
        }

        public byte[] ToByteArray()
        {
            if (this.Value == null)
            {
                return null;
            }

            if (this.Value.GetType() == typeof(byte[]))
            {
                return (byte[])this.Value;
            }

            return new RegistryHelper().StringToByteArray(this.ToString());
        }

    }
}
