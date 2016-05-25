using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegistryClass
{
    public class RegKey
    {
        private RegistryHelper.ROOT_KEY RootKey;
        public string SubKey;

        public bool ReadValues { get; set; }

        public bool ReadSubkeys { get; set; }

        public string KeyName { get; set; }
        public RegistryHelper oParentObject { set; get; }

        public List<RegObject> Values { get; set; }
        public List<RegKey> SubKeys { get; set; }


        public RegKey(RegistryHelper registryHelper, RegistryHelper.ROOT_KEY RootKey, string SubKey, bool ReadValues, bool ReadSubkeys)
        {
            if (!String.IsNullOrEmpty(SubKey))
            {
                this.KeyName = SubKey.Substring(SubKey.LastIndexOf("\\") + 1);
            }

            if (!String.IsNullOrEmpty(SubKey) && SubKey.Substring(0, 1) == "\\")
            {
                SubKey = SubKey.Substring(1);
            }

            if (this.oParentObject == null)
            {
                this.oParentObject = registryHelper;
            }

            this.RootKey = RootKey;
            this.SubKey = SubKey;
            this.ReadValues = ReadValues;
            this.ReadSubkeys = ReadSubkeys;

            if (this.ReadValues)
            {
                this.readValues();
            }

            if (this.ReadSubkeys)
            {
                this.readKeys();
            }
        }

        internal void readValues()
        {
            this.Values = oParentObject.GetValues(this.RootKey,this.SubKey);
        }

        internal void readKeys()
        {
            this.SubKeys = new List<RegKey>();

            foreach (string sKeyName in oParentObject.EnumKeys(this.RootKey, this.SubKey))
            {
                SubKeys.Add(this.oParentObject.GetKey(this.RootKey, this.SubKey + "\\" + sKeyName, this.ReadValues, this.ReadSubkeys));
            }
        }

      
    }
}
