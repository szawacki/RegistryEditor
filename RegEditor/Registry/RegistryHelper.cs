using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RegistryClass
{
    [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
    public class RegistryHelper
    {
        #region Constants
        // access paramaters  
        private const int KEY_ALL_ACCESS = 0xF003F;
        private const int KEY_CREATE_LINK = 0x20;
        private const int KEY_CREATE_SUB_KEY = 0x4;
        private const int KEY_ENUMERATE_SUB_KEYS = 0x8;
        private const int KEY_EXECUTE = 0x20019;
        private const int KEY_NOTIFY = 0x10;
        private const int KEY_QUERY_VALUE = 0x1;
        private const int KEY_READ = 0x20019;
        private const int KEY_SET_VALUE = 0x2;
        private const int KEY_WRITE = 0x20006;
        private const int REG_OPTION_NON_VOLATILE = 0x0;
        private const int REG_ERR_OK = 0x0;
        private const int REG_ERR_NOT_EXIST = 0x1;
        private const int REG_ERR_NOT_STRING = 0x2;
        private const int REG_ERR_NOT_DWORD = 0x4;

        // error handling  
        private const int ERROR_NONE = 0x0;
        private const int ERROR_BADDB = 0x1;
        private const int ERROR_BADKEY = 0x2;
        private const int ERROR_CANTOPEN = 0x3;
        private const int ERROR_CANTREAD = 0x4;
        private const int ERROR_CANTWRITE = 0x5;
        private const int ERROR_OUTOFMEMORY = 0x6;
        private const int ERROR_ARENA_TRASHED = 0x7;
        private const int ERROR_ACCESS_DENIED = 0x8;
        private const int ERROR_INVALID_PARAMETERS = 0x57;
        private const int ERROR_MORE_DATA = 0xEA;
        private const int ERROR_NO_MORE_ITEMS = 0x103;

        // token  
        private const int SE_PRIVILEGE_ENABLED = 0x2;
        private const int SYNCHRONIZE = 0x100000;
        private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        private const int PROCESS_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF);

        // privilege  
        private const string SE_CREATE_TOKEN_NAME = "SeCreateTokenPrivilege";
        private const string SE_ASSIGNPRIMARYTOKEN_NAME = "SeAssignPrimaryTokenPrivilege";
        private const string SE_TCB_NAME = "SeTcbPrivilege";
        private const string SE_SECURITY_NAME = "SeSecurityPrivilege";
        private const string SE_DEBUG_NAME = "SeDebugPrivilege";
        private const string SE_AUDIT_NAME = "SeAuditPrivilege";
        #endregion

        #region Enums
        // root keys  
        public enum ROOT_KEY : uint
        {
            HKEY_CLASSES_ROOT = 0x80000000,
            HKEY_CURRENT_USER = 0x80000001,
            HKEY_LOCAL_MACHINE = 0x80000002,
            HKEY_USERS = 0x80000003,
            HKEY_PERFORMANCE_DATA = 0x80000004,
            HKEY_CURRENT_CONFIG = 0x80000005,
            HKEY_DYN_DATA = 0x80000006
        }

        // value types  
        public enum VALUE_TYPE : uint
        {
            REG_NONE,
            REG_SZ = 1,
            REG_EXPAND_SZ = 2,
            REG_BINARY = 3,
            REG_DWORD = 4,
            REG_DWORD_LITTLE_ENDIAN = 4,
            REG_DWORD_BIG_ENDIAN = 5,
            REG_LINK = 6,
            REG_MULTI_SZ = 7,
            REG_RESOURCE_LIST = 8,
            REG_FULL_RESOURCE_DESCRIPTOR = 9,
            REG_RESOURCE_REQUIREMENTS_LIST = 10,
            REG_QWORD = 11,
            REG_QWORD_LITTLE_ENDIAN = 11,
            REG_ANY = 65535
        }

        // token privileges  
        private enum ETOKEN_PRIVILEGES : uint
        {
            ASSIGN_PRIMARY = 0x1,
            TOKEN_DUPLICATE = 0x2,
            TOKEN_IMPERSONATE = 0x4,
            TOKEN_QUERY = 0x8,
            TOKEN_QUERY_SOURCE = 0x10,
            TOKEN_ADJUST_PRIVILEGES = 0x20,
            TOKEN_ADJUST_GROUPS = 0x40,
            TOKEN_ADJUST_DEFAULT = 0x80,
            TOKEN_ADJUST_SESSIONID = 0x100
        }

        public ROOT_KEY lastRootKey { get; set; }
        #endregion

        #region Structs
        // filetime (unused)  
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct FILETIME
        {
            public int dwLowDateTime;
            public int dwHighDateTime;
        }

        // permissions  
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public int lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        // perm luid  
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct LUID
        {
            public int LowPart;
            public int HighPart;
        }

        // perm attributes  
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct LUID_AND_ATTRIBUTES
        {
            public LUID pLuid;
            public int Attributes;
        }

        // perm token  
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privileges;
        }
        #endregion

        #region API
        // enable priviledge decl  
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "AdjustTokenPrivileges", SetLastError = true)]
        private static extern int AdjustTokenPrivileges(IntPtr TokenHandle, int DisableAllPriv, ref TOKEN_PRIVILEGES NewState, int BufferLength, int PreviousState, int ReturnLength);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "LookupPrivilegeValueW", SetLastError = true)]
        private static extern int LookupPrivilegeValue(int lpSystemName, [MarshalAs(UnmanagedType.LPWStr)]string lpName, ref LUID lpLuid);
        //[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "LookupPrivilegeValueA", SetLastError = true)]
        //private static extern int LookupPrivilegeValueA(int lpSystemName, [MarshalAs(UnmanagedType.LPStr)]string lpName, ref LUID lpLuid);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenProcessToken", SetLastError = true)]
        private static extern int OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, ref IntPtr TokenHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "OpenProcess", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, int blnheritHandle, int dwAppProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CloseHandle", SetLastError = true)]
        private static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetCurrentProcessId", SetLastError = true)]
        private static extern int GetCurrentProcessId();

        //[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "RtlMoveMemory", SetLastError = true)]
        //private static extern int RtlMoveMemory(ref string Destination, ref byte[] Source, int Length);

        // RegQueryValueEx overloads- added ansi decl versions for old platforms [untested AND no methods provided]  
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        static extern int RegQueryValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int lpReserved, out uint lpType, [Optional] System.Text.StringBuilder lpData, ref uint lpcbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueEx", SetLastError = true)]
        //static extern int RegQueryValueEx2(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int lpReserved, out uint lpType, [Optional] System.Text.StringBuilder lpData, ref uint lpcbData);
        
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegQueryValueExA", SetLastError = true)]
        //static extern int RegQueryValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int lpReserved, out uint lpType, [Optional] [MarshalAs(UnmanagedType.LPStr)]string lpData, ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        static extern int RegQueryValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int lpReserved, ref uint lpType, [Optional] ref byte lpData, ref uint lpcbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegQueryValueExA", SetLastError = true)]
        //static extern int RegQueryValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int lpReserved, ref uint lpType, [Optional] ref byte lpData, ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        static extern int RegQueryValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int lpReserved, ref uint lpType, [Optional] ref uint lpData, ref uint lpcbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegQueryValueExA", SetLastError = true)]
        //static extern int RegQueryValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int lpReserved, ref uint lpType, [Optional] ref int lpData, ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        static extern int RegQueryValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int lpReserved, ref uint lpType, [Optional] ref ulong lpData, ref uint lpcbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegQueryValueExA", SetLastError = true)]
        //static extern int RegQueryValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int lpReserved, ref uint lpType, [Optional] ref long lpData, ref uint lpcbData);

        // RegSetValueEx overloads  
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegSetValueExW", SetLastError = true)]
        private static extern int RegSetValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int Reserved, uint dwType, ref uint lpData, int cbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegSetValueExA", SetLastError = true)]
        //private static extern int RegSetValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int Reserved, uint dwType, ref int lpData, int cbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegSetValueExW", SetLastError = true)]
        private static extern int RegSetValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int Reserved, uint dwType, ref ulong lpData, int cbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegSetValueExA", SetLastError = true)]
        //private static extern int RegSetValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int Reserved, uint dwType, ref long lpData, int cbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegSetValueExW", SetLastError = true)]
        private static extern int RegSetValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int Reserved, uint dwType, IntPtr lpData, int cbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegSetValueEA", SetLastError = true)]
        //private static extern int RegSetValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int Reserved, uint dwType, [MarshalAs(UnmanagedType.LPStr)]string lpData, int cbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegSetValueExW", SetLastError = true)]
        private static extern int RegSetValueEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName, int Reserved, uint dwType, ref byte lpData, int cbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegSetValueExA", SetLastError = true)]
        //private static extern int RegSetValueExA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, int Reserved, uint dwType, ref byte lpData, int cbData);

        // registry  
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegCloseKey", SetLastError = true)]
        private static extern int RegCloseKey(UIntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegCreateKeyW", SetLastError = true)]
        private static extern int RegCreateKey(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPWStr)]string subKey, ref UIntPtr phkResult);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegCreateKeyA", SetLastError = true)]
        //private static extern int RegCreateKeyA(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPStr)]string subKey, ref UIntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyExW", SetLastError = true)]
        private static extern int RegOpenKeyEx(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPWStr)]string subKey, int options, int samDesired, ref UIntPtr phkResult);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegOpenKeyExA", SetLastError = true)]
        //private static extern int RegOpenKeyExA(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPStr)]string subKey, int options, int samDesired, ref UIntPtr phkResult);

        //[DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyExW", SetLastError = true)]
        //private static extern int RegOpenKeyEx(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string subKey, int options, int samDesired, ref UIntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegDeleteKeyA", SetLastError = true)]
        private static extern int RegDeleteKeyA(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPStr)]string subKey);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegDeleteKeyW", SetLastError = true)]
        //private static extern int RegDeleteKey(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPWStr)]string subKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegDeleteValueW", SetLastError = true)]
        private static extern int RegDeleteValue(UIntPtr hKey, [MarshalAs(UnmanagedType.LPWStr)]string lpValueName);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegDeleteValueA", SetLastError = true)]
        //private static extern int RegDeleteValueA(UIntPtr hKey, [MarshalAs(UnmanagedType.LPStr)]string lpValueName);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegCreateKeyExW", SetLastError = true)]
        private static extern int RegCreateKeyEx(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPWStr)]string subKey, int Reserved, [MarshalAs(UnmanagedType.LPWStr)]string lpClass, int dwOptions, int samDesired, ref SECURITY_ATTRIBUTES lpSecurityAttributes, ref UIntPtr phkResult, ref int lpdwDisposition);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegCreateKeyExA", SetLastError = true)]
        //private static extern int RegCreateKeyExA(ROOT_KEY hKey, [MarshalAs(UnmanagedType.LPStr)]string subKey, int Reserved, [MarshalAs(UnmanagedType.LPStr)]string lpClass, int dwOptions, int samDesired, ref SECURITY_ATTRIBUTES lpSecurityAttributes, ref UIntPtr phkResult, ref int lpdwDisposition);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegEnumKeyExW", SetLastError = true)]
        private static extern int RegEnumKeyEx(UIntPtr hKey, uint index, StringBuilder lpName, ref uint lpcbName, IntPtr reserved, IntPtr lpClass, IntPtr lpcbClass, out long lpftLastWriteTime);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegEnumKeyExA", SetLastError = true)]
        //private static extern int RegEnumKeyExA(UIntPtr hKey, uint index, [MarshalAs(UnmanagedType.LPStr)]string lpName, ref uint lpcbName, IntPtr reserved, IntPtr lpClass, IntPtr lpcbClass, out long lpftLastWriteTime);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegEnumValueW", SetLastError = true)]
        private static extern int RegEnumValue(UIntPtr hKey, uint dwIndex, StringBuilder lpValueName, ref uint lpcValueName, IntPtr lpReserved, IntPtr lpType, IntPtr lpData, IntPtr lpcbData);
        //[DllImport("advapi32.dll", CharSet = CharSet.Ansi, EntryPoint = "RegEnumValueA", SetLastError = true)]
        //private static extern int RegEnumValueA(UIntPtr hKey, uint dwIndex, [MarshalAs(UnmanagedType.LPStr)]string lpValueName, ref uint lpcValueName, IntPtr lpReserved, IntPtr lpType, IntPtr lpData, IntPtr lpcbData);

         //remote registry
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegConnectRegistry(string lpmachineName, ROOT_KEY hKey, ref ROOT_KEY phKResult);
        #endregion

        #region Generic read and write

        /// <summary>
        /// Reads registry keys without knowledge about the key type.<para/>
        /// Returns a RegObject containing RootKey, SubKey KeyType, KeyName, Value.<para/>
        /// Value types: REG_SZ = string | REG_DWORD = uint | REG_QWORD = ulong | REG_BINARY = byte[] | REG_EYPAND_SZ = string | error = null<para/>
        /// </summary>
        /// <param name="RootKey">RootKey type of ROOT_KEY</param>
        /// <param name="SubKey">SubKey type of string</param>
        /// <param name="Value">Value type of string</param>
        /// <returns>RegObject</returns>
        public RegObject ReadValue(ROOT_KEY RootKey, string SubKey, string Value)
        {
            RegObject oReg = new RegObject();
            object value = null;
            UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 0;
            uint pdwType = (uint)VALUE_TYPE.REG_ANY;

            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, out pdwType, null, ref pvSize) == ERROR_NONE)
                    {
                        switch ((VALUE_TYPE)pdwType)
                        {
                            case VALUE_TYPE.REG_BINARY:
                            {
                                value = this.ReadBinary(RootKey, SubKey, Value);
                                break;
                            }
                            case VALUE_TYPE.REG_DWORD:
                            {
                                value = this.ReadDword(RootKey, SubKey, Value);
                                break;
                            }
                            case VALUE_TYPE.REG_EXPAND_SZ:
                            {
                                value = this.ReadExpand(RootKey, SubKey, Value);
                                break;
                            }
                            case VALUE_TYPE.REG_MULTI_SZ:
                            {
                                value = this.ReadMulti(RootKey, SubKey, Value);
                                break;
                            }
                            case VALUE_TYPE.REG_SZ:
                            {
                                value = this.ReadString(RootKey, SubKey, Value);
                                break;
                            }
                            case VALUE_TYPE.REG_QWORD:
                            {
                                value = this.ReadQword(RootKey, SubKey, Value);
                                break;
                            }
                            case VALUE_TYPE.REG_LINK:
                            {
                                value = this.ReadLink(RootKey, SubKey, Value);
                                break;
                            }
                            case VALUE_TYPE.REG_RESOURCE_LIST:
                            {
                                value = this.ReadResourceList(RootKey, SubKey, Value);
                                break;
                            }
                            default:
                                break;
                        }
                    }
                }
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }

            oReg.Value = value;
            oReg.Type = (VALUE_TYPE)pdwType;
            oReg.SubKey = SubKey;
            oReg.RootKey = RootKey;
            oReg.KeyName = Value;

            return oReg;
        }


        /// <summary>
        /// Get all SuvValues as Objectlist
        /// </summary>
        /// <param name="RootKey">RootKey type of ROOT_KEY</param>
        /// <param name="SubKey">SubKey type of string</param>
        /// <returns>List RegObject over all SubValues</returns>
        public List<RegObject> GetValues(ROOT_KEY RootKey, string SubKey)
        {
            List<RegObject> Result = new List<RegObject>();

            foreach (string sValue in EnumValues(RootKey, SubKey))
            {
                Result.Add(ReadValue(RootKey, SubKey, sValue));
            }

            return Result;
        }

        internal RegKey GetKey(ROOT_KEY root, string sKeyname)
        {
            return GetKey(root, sKeyname, false, false);
        }

        public RegKey GetKey(ROOT_KEY RootKey, string SubKey, bool ReadValues,bool ReadSubkeys)
        {
            RegKey Result = new RegKey(this,RootKey, SubKey,ReadValues,ReadSubkeys);

            return Result;
        }

        /// <summary>
        /// Write value to registry
        /// </summary>
        /// <param name="RootKey">ROOT_KEY</param>
        /// <param name="SubKey">SubKey type of string</param>
        /// <param name="type">VALUE_TYPE</param>
        /// <param name="Value">Value type of string</param>
        /// <param name="Data">Data type of object. REG_SZ = string | REG_DWORD = uint | REG_QWORD = ulong | REG_BINARY = byte[] | REG_EYPAND_SZ = string</param>
        /// <returns>bool</returns>
        public bool WriteValue(ROOT_KEY RootKey, string SubKey, VALUE_TYPE type, string Value, object Data)
        {
            bool result = false;
            UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)type;

            RootKey = this.Regconnect(this.hostname, RootKey);
            try
            {
                if (this.CreateKey(RootKey, SubKey))
                {
                    switch (type)
                    {
                        case VALUE_TYPE.REG_BINARY:
                        {
                            byte[] nbarr = (byte[])Data;
                            result = this.WriteBinary(RootKey, SubKey, Value, ref nbarr);
                            break;
                        }
                        case VALUE_TYPE.REG_LINK:
                        {
                            byte[] nlarr = (byte[])Data;
                            result = this.WriteLink(RootKey, SubKey, Value, ref nlarr);
                            break;
                        }
                        case VALUE_TYPE.REG_RESOURCE_LIST:
                        {
                            byte[] nrarr = (byte[])Data;
                            result = this.WriteResourceList(RootKey, SubKey, Value, ref nrarr);
                            break;
                        }
                        case VALUE_TYPE.REG_DWORD:
                        {
                            try
                            {
                                uint val = Convert.ToUInt32(Data);
                                result = this.WriteDword(RootKey, SubKey, Value, val);
                            }
                            catch
                            {
                                return false;
                            }
                            break;
                        }
                        case VALUE_TYPE.REG_EXPAND_SZ:
                        {
                            result = this.WriteExpand(RootKey, SubKey, Value, Convert.ToString(Data));
                            break;
                        }
                        case VALUE_TYPE.REG_MULTI_SZ:
                        {
                            result = this.WriteMulti(RootKey, SubKey, Value, Data as string[]);
                            break;
                        }
                        case VALUE_TYPE.REG_SZ:
                        {
                            result = this.WriteString(RootKey, SubKey, Value, Convert.ToString(Data));
                            break;
                        }
                        case VALUE_TYPE.REG_QWORD:
                        {
                            try
                            {
                                ulong val = Convert.ToUInt64(Data);
                                result = this.WriteQword(RootKey, SubKey, Value, val);
                            }
                            catch
                            {
                                return false;
                            }
                            break;
                        }
                        default:
                            break;
                    }

                    return result;
                }
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }

            return result;
        }
        #endregion

        #region Permissions
        /// <summary>  
        /// Test registry access permissions  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <returns>bool</returns>  
        public bool AccessTest(ROOT_KEY RootKey, string SubKey)
        {
            UIntPtr pHKey = UIntPtr.Zero;
            int lDeposit = 0;
            SECURITY_ATTRIBUTES tSecAttrib = new SECURITY_ATTRIBUTES();

            try
            {
                // security attributes  
                tSecAttrib.nLength = Marshal.SizeOf(tSecAttrib);
                tSecAttrib.lpSecurityDescriptor = 0;
                tSecAttrib.bInheritHandle = true;
                // open key  
                return (RegCreateKeyEx(RootKey, SubKey, 0, "", 0, KEY_WRITE, ref tSecAttrib, ref pHKey, ref lDeposit) == ERROR_NONE);
            }
            finally
            {
                // close key  
                if (pHKey != UIntPtr.Zero)
                {
                    RegCloseKey(pHKey);
                }
            }

        }

        /// <summary>  
        /// Enable/disable debug level access  
        /// </summary>  
        /// <param name="Enable">bool: enable/disable</param>  
        /// <returns>bool</returns>  
        public bool EnableAccess(bool Enable)
        {
            IntPtr hToken = IntPtr.Zero;
            IntPtr hProcess = IntPtr.Zero;
            LUID tLuid = new LUID();
            TOKEN_PRIVILEGES NewState = new TOKEN_PRIVILEGES();
            uint uPriv = (uint)(ETOKEN_PRIVILEGES.TOKEN_ADJUST_PRIVILEGES | ETOKEN_PRIVILEGES.TOKEN_QUERY | ETOKEN_PRIVILEGES.TOKEN_QUERY_SOURCE);

            try
            {
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, 0, GetCurrentProcessId());
                if (hProcess == IntPtr.Zero)
                {
                    return false;
                }

                if (OpenProcessToken(hProcess, uPriv, ref hToken) == 0)
                {
                    return false;
                }

                // Get the local unique id for the privilege.  
                if (LookupPrivilegeValue(0, SE_DEBUG_NAME, ref tLuid) == 0)
                {
                    return false;
                }

                // Assign values to the TOKEN_PRIVILEGE structure.  
                NewState.PrivilegeCount = 1;
                NewState.Privileges.pLuid = tLuid;
                NewState.Privileges.Attributes = (Enable ? SE_PRIVILEGE_ENABLED : 0);
                // Adjust the token privilege.  
                return (AdjustTokenPrivileges(hToken, 0, ref NewState, Marshal.SizeOf(NewState), 0, 0) != 0);
            }
            finally
            {
                if (hToken != IntPtr.Zero)
                {
                    CloseHandle(hToken);
                }

                if (hProcess != IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                }
            }
        }
        #endregion

        #region BigEndian
        /// <summary>  
        /// Read BigEndian integer type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>int: value / -1 on fail</returns>  
        private uint ReadBigEndian(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 4;
            uint pdwType = (uint)VALUE_TYPE.REG_DWORD_BIG_ENDIAN;
            uint pvData = 0;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                // open root key  
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref pvData, ref pvSize) == ERROR_NONE)
                    {
                        // return int  
                        return pvData;
                    }
                }
                return new uint { };
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a BigEndian integer  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">int: data</param>  
        /// <returns>bool</returns>  
        private bool WriteBigEndian(ROOT_KEY RootKey, string SubKey, string Value, uint Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_DWORD_BIG_ENDIAN;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                // open root key  
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data, 4) == ERROR_NONE)
                    {
                        // return status  
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region Binary
        /// <summary>  
        /// Read Binary data  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string: value / empty on fail</returns>  
        private byte[] ReadBinary(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 1024;
            uint pdwType = (uint)VALUE_TYPE.REG_BINARY;
            byte[] bBuffer = new byte[1024];
            byte[] resultByteArr = new byte[0];
            string sRet = String.Empty;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                // open root key  
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref bBuffer[0], ref pvSize) == ERROR_NONE)
                    {
                        resultByteArr = new byte[pvSize];
                        resultByteArr = bBuffer.Take((int)pvSize).ToArray();
                    }
                    else
                    {
                        return new byte[0];
                    }
                }
                else
                {
                    return new byte[0];
                }
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
            return resultByteArr;
        }

        /// <summary>  
        /// Write a Binary value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">byte array: data</param>  
        /// <returns>bool</returns>  
        private bool WriteBinary(ROOT_KEY RootKey, string SubKey, string Value, ref byte[] Data)
        {
            UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_BINARY;
            RootKey = this.Regconnect(this.hostname, RootKey);
            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (Data.Length == 0)
                    {
                        byte b = new byte();
                        if (RegSetValueEx(hKey, Value, 0, pdwType, ref b, 0) == ERROR_NONE)
                        {
                            return true;
                        }
                    }

                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data[0], (Data.GetUpperBound(0) + 1)) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region DWord
        /// <summary>  
        /// Read Dword integer type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>int: value / -1 on fail</returns>  
        private uint ReadDword(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 4;
            uint pdwType = (uint)VALUE_TYPE.REG_DWORD;
            uint pvData = 0;
            RootKey = this.Regconnect(this.hostname, RootKey);
            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref pvData, ref pvSize) == ERROR_NONE)
                    {
                        return pvData;
                    }
                }
                return new uint { };
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a Dword value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">int: data</param>  
        /// <returns>bool</returns>  
        private bool WriteDword(ROOT_KEY RootKey, string SubKey, string Value, uint Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_DWORD;
            RootKey = this.Regconnect(this.hostname, RootKey);
            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data, 4) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region Expanded
        /// <summary>  
        /// Read Expanded String  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string: value / empty on fail</returns>  
        private string ReadExpand(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            System.Text.StringBuilder pvData = new System.Text.StringBuilder(0);
            uint pvSize = 0;
            uint pdwType = (uint)VALUE_TYPE.REG_EXPAND_SZ;
            RootKey = this.Regconnect(this.hostname, RootKey);
            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, out pdwType, null, ref pvSize) == ERROR_NONE)
                    {
                        pvData = new System.Text.StringBuilder((int)(pvSize / 2));
                        RegQueryValueEx(hKey, Value, 0, out pdwType, pvData, ref pvSize);
                    }
                }
                return pvData.ToString();
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a Expanded String value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">string: data</param>  
        /// <returns>bool</returns>  
        private bool WriteExpand(ROOT_KEY RootKey, string SubKey, string Value, string Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_EXPAND_SZ;
            IntPtr pStr = IntPtr.Zero;
            RootKey = this.Regconnect(this.hostname, RootKey);
            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    int Size = (Data.Length + 1) * Marshal.SystemDefaultCharSize;
                    pStr = Marshal.StringToHGlobalAuto(Data);
                    if (RegSetValueEx(hKey, Value, 0, pdwType, pStr, Size) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
                if (pStr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pStr);
                }
            }
        }
        #endregion

        #region Link
        /// <summary>  
        /// Read Link data type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string: value / empty on fail</returns>  
        private string ReadLink(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 1024;
            uint pdwType = (uint)VALUE_TYPE.REG_LINK;
            byte[] bBuffer = new byte[1024];
            string sRet = String.Empty;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref bBuffer[0], ref pvSize) == ERROR_NONE)
                    {
                        for (int i = 0; i < (pvSize); i++)
                        {
                            sRet += bBuffer[i].ToString();
                        }
                    }
                }
                return sRet;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a Link value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">byte array: data</param>  
        /// <returns>bool</returns>  
        private bool WriteLink(ROOT_KEY RootKey, string SubKey, string Value, ref byte[] Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_LINK;
            RootKey = this.Regconnect(this.hostname, RootKey);
            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (Data.Length == 0)
                    {
                        byte b = new byte();
                        if (RegSetValueEx(hKey, Value, 0, pdwType, ref b, 0) == ERROR_NONE)
                        {
                            return true;
                        }
                    }

                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data[0], (Data.GetUpperBound(0) + 1)) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region Multi
        /// <summary>  
        /// Read Multi String data type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string[]: value</returns>  
        private string[] ReadMulti(ROOT_KEY RootKey, string SubKey, string Value)
        {         
            UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 0;
            uint pdwType = (uint)VALUE_TYPE.REG_MULTI_SZ;
            RootKey = this.Regconnect(this.hostname, RootKey);
            string[] result = null;

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, out pdwType, null, ref pvSize) == ERROR_NONE)
                    {
                        string sRet = String.Empty;

                        if ((VALUE_TYPE)pdwType == VALUE_TYPE.REG_MULTI_SZ)
                        {
                            if (pvSize == 0)
                            {
                                return new string[] { "" };
                            }

                            byte[] bBuffer = new byte[pvSize];

                            if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref bBuffer[0], ref pvSize) == ERROR_NONE)
                            {
                                sRet = this.ByteArrayToString(bBuffer);
                            }

                            result = sRet.Replace("\0\0\0\0", "").Split(new string[] { "\0\0" }, StringSplitOptions.None);

                            for (int i = 0; i < result.Length; i++)
                            {
                                result[i] = result[i].Replace("\0", "");
                            }
                        }
                    }
                }
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
            return result;
        }

        /// <summary>  
        /// Write a Multi String value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">string[]: data</param>  
        /// <returns>bool</returns>  
        private bool WriteMulti(ROOT_KEY RootKey, string SubKey, string Value, string[] Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_MULTI_SZ;
            IntPtr pStr = IntPtr.Zero;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    string stringData = "";

                    foreach (string str in Data)
                    {
                        stringData += str + "\0";
                    }

                    int Size = (stringData.Length + 1) * Marshal.SystemDefaultCharSize;
                    pStr = Marshal.StringToHGlobalAuto(stringData);

                    if (RegSetValueEx(hKey, Value, 0, pdwType, pStr, Size) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }

                if (pStr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pStr);
                }
            }
        }
        #endregion

        #region Qword
        /// <summary>  
        /// Read Qword large integer type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>ulong / null on fail</returns>  
        private ulong ReadQword(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 8;
            uint pdwType = (uint)VALUE_TYPE.REG_QWORD_LITTLE_ENDIAN;
            ulong pvData = 0;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref pvData, ref pvSize) == ERROR_NONE)
                    {
                        return pvData;
                    }
                }
                return new ulong { };
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a Qword value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">long: data</param>  
        /// <returns>bool</returns>  
        private bool WriteQword(ROOT_KEY RootKey, string SubKey, string Value, ulong Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_QWORD_LITTLE_ENDIAN;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data, 8) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region Resource Descriptor
        /// <summary>  
        /// Read Resource Descriptor data type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string: value / empty on fail</returns>  
        private string ReadResourceDescriptor(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 1024;
            uint pdwType = (uint)VALUE_TYPE.REG_FULL_RESOURCE_DESCRIPTOR;
            byte[] bBuffer = new byte[1024];
            string sRet = String.Empty;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref bBuffer[0], ref pvSize) == ERROR_NONE)
                    {
                        for (int i = 0; i < (pvSize); i++)
                        {
                            sRet += bBuffer[i].ToString();
                        }
                    }
                }
                return sRet;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a Resource Descriptor value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">byte array: data</param>  
        /// <returns>bool</returns>  
        private bool WriteResourceDescriptor(ROOT_KEY RootKey, string SubKey, string Value, ref byte[] Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_FULL_RESOURCE_DESCRIPTOR;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (Data.Length == 0)
                    {
                        byte b = new byte();

                        if (RegSetValueEx(hKey, Value, 0, pdwType, ref b, 0) == ERROR_NONE)
                        {
                            return true;
                        }
                    }

                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data[0], (Data.GetUpperBound(0) + 1)) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region Resource List
        /// <summary>  
        /// Read Resource List data type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string: value / empty on fail</returns>  
        private string ReadResourceList(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 1024;
            uint pdwType = (uint)VALUE_TYPE.REG_RESOURCE_LIST;
            byte[] bBuffer = new byte[1024];
            string sRet = String.Empty;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref bBuffer[0], ref pvSize) == ERROR_NONE)
                    {
                        for (int i = 0; i < (pvSize); i++)
                        {
                            sRet += bBuffer[i].ToString();
                        }
                    }
                }
                return sRet;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a Resource List value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">byte array: data</param>  
        /// <returns>bool</returns>  
        private bool WriteResourceList(ROOT_KEY RootKey, string SubKey, string Value, ref byte[] Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_RESOURCE_LIST;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (Data.Length == 0)
                    {
                        byte b = new byte();

                        if (RegSetValueEx(hKey, Value, 0, pdwType, ref b, 0) == ERROR_NONE)
                        {
                            return true;
                        }
                    }

                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data[0], (Data.GetUpperBound(0) + 1)) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region Resource Requirements
        /// <summary>  
        /// Read Requirements data type  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string: value / empty on fail</returns>  
        private string ReadResourceRequirements(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 1024;
            uint pdwType = (uint)VALUE_TYPE.REG_RESOURCE_REQUIREMENTS_LIST;
            byte[] bBuffer = new byte[1024];
            string sRet = String.Empty;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, ref pdwType, ref bBuffer[0], ref pvSize) == ERROR_NONE)
                    {
                        for (int i = 0; i < (pvSize); i++)
                        {
                            sRet += bBuffer[i].ToString();
                        }
                    }
                }
                return sRet;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a Resource Requirements value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">byte array: data</param>  
        /// <returns>bool</returns>  
        private bool WriteResourceRequirements(ROOT_KEY RootKey, string SubKey, string Value, ref byte[] Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_RESOURCE_REQUIREMENTS_LIST;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (Data.Length == 0)
                    {
                        byte b = new byte();

                        if (RegSetValueEx(hKey, Value, 0, pdwType, ref b, 0) == ERROR_NONE)
                        {
                            return true;
                        }
                    }

                    if (RegSetValueEx(hKey, Value, 0, pdwType, ref Data[0], (Data.GetUpperBound(0) + 1)) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }
        #endregion

        #region String
        /// <summary>  
        /// Read String  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>string: value / empty on fail</returns>  
        private string ReadString(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            System.Text.StringBuilder pvData = new System.Text.StringBuilder(0);
            uint pvSize = 0;
            uint pdwType = (uint)VALUE_TYPE.REG_SZ;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_READ, ref hKey) == ERROR_NONE)
                {
                    if (RegQueryValueEx(hKey, Value, 0, out pdwType, null, ref pvSize) == ERROR_NONE)
                    {
                        pvData = new System.Text.StringBuilder((int)(pvSize / 2));
                        RegQueryValueEx(hKey, Value, 0, out pdwType, pvData, ref pvSize);
                    }
                }
                return pvData.ToString();
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Write a String value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <param name="Data">string: data</param>  
        /// <returns>bool</returns>  
        private bool WriteString(ROOT_KEY RootKey, string SubKey, string Value, string Data)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pdwType = (uint)VALUE_TYPE.REG_SZ;
            IntPtr pStr = IntPtr.Zero;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    int Size = (Data.Length + 1) * Marshal.SystemDefaultCharSize;
                    pStr = Marshal.StringToHGlobalAuto(Data);

                    if (RegSetValueEx(hKey, Value, 0, pdwType, pStr, Size) == ERROR_NONE)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }

                if (pStr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pStr);
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>  
        /// Test key presence  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <returns>bool</returns>  
        public bool KeyExists(ROOT_KEY RootKey, string SubKey)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                return (RegOpenKeyEx(RootKey, SubKey, 0, KEY_QUERY_VALUE, ref hKey) == ERROR_NONE);
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Test value presence  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>bool</returns>  
        public bool ValueExists(ROOT_KEY RootKey, string SubKey, string Value)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            uint pvSize = 0;
            uint pdwType = (uint)VALUE_TYPE.REG_NONE;
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_QUERY_VALUE, ref hKey) != ERROR_NONE)
                {
                    return false;
                }

                return (RegQueryValueEx(hKey, Value, 0, out pdwType, null, ref pvSize) == ERROR_NONE);
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Create a named key  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <returns>bool</returns>  
        public bool CreateKey(ROOT_KEY RootKey, string SubKey)
        {
            //UIntPtr hKey = UIntPtr.Zero;
            try
            {
                return (RegCreateKey(RootKey, SubKey, ref hKey) == ERROR_NONE);
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        /// <summary>  
        /// Delete a key  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <returns>bool</returns>  
        public bool DeleteKey(ROOT_KEY RootKey, string SubKey)
        {
            try
            {
                foreach (String sSubkeyName in EnumKeys(RootKey, SubKey))
                {
                    if (!DeleteKey(RootKey, SubKey + @"\" + sSubkeyName))
                    {
                        return false;
                    }
                }

                if (!KeyExists(RootKey, SubKey))
                {
                    return true;
                }
                
                return (RegDeleteKeyA(RootKey, SubKey) == ERROR_NONE);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>  
        /// Delete a value  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <param name="Value">string: named value</param>  
        /// <returns>bool</returns>  
        public bool DeleteValue(ROOT_KEY RootKey, string SubKey, string Value)
        {
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_WRITE, ref hKey) == ERROR_NONE)
                {
                    if (RegDeleteValue(hKey, Value) == ERROR_NONE)
                    {
                        return true;
                    }
                }
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
            return false;
        }

        private UIntPtr hKey = UIntPtr.Zero;
        private ROOT_KEY rootHKey;
        private string hostname;

        public RegistryHelper(string hostname)
        {
            this.hostname = hostname;         
        }

        public RegistryHelper()
        {
            this.hostname = null;
        }

        public ROOT_KEY Regconnect(string rechnername, ROOT_KEY Rootkey)
        {
            if (!String.IsNullOrEmpty(this.hostname))
            {
                if (this.lastRootKey != Rootkey)
                {
                    this.lastRootKey = Rootkey;
                    RegConnectRegistry(rechnername, Rootkey, ref this.rootHKey);
                    Rootkey = this.rootHKey;
                }
            }
            return Rootkey;
        }

        /// <summary>  
        /// Enumerate and collect keys  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <returns>ArrayList</returns>  
        public ArrayList EnumKeys(ROOT_KEY RootKey, string SubKey)
        {
            uint keyLen = 255;
            uint index = 0;
            int ret = 0;
            long lastWrite = 0;
            //UIntPtr hKey = UIntPtr.Zero;
            StringBuilder keyName = new StringBuilder(0);
            ArrayList keyList = new ArrayList();

            RootKey = this.Regconnect(this.hostname, RootKey);
            
            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_ENUMERATE_SUB_KEYS, ref hKey) != ERROR_NONE)
                {
                    return keyList;
                }

                do
                {
                    keyLen = 255;
                    keyName = new StringBuilder(255);
                    ret = RegEnumKeyEx(this.hKey, index, keyName, ref keyLen, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out lastWrite);

                    if (ret == ERROR_NONE)
                    {
                        keyList.Add(keyName.ToString());
                    }

                    index += 1;
                }
                while (ret == 0);

                return keyList;
            }
            catch //(Exception ex)
            {
                return null;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    try
                    {
                        RegCloseKey(hKey);
                    }
                    catch //(Exception ex)
                    {
                        // Skip if failure during close event
                    }
                }
            }
        }

        /// <summary>  
        /// Enumerate and collect values  
        /// </summary>  
        /// <param name="RootKey">enum: root key</param>  
        /// <param name="SubKey">string: named subkey</param>  
        /// <returns>ArrayList</returns>  
        public ArrayList EnumValues(ROOT_KEY RootKey, string SubKey)
        {
            uint valLen = 255;
            uint index = 0;
            int ret = 0;
            //UIntPtr hKey = UIntPtr.Zero;
            StringBuilder valName = new StringBuilder(0);
            ArrayList valList = new ArrayList();
            RootKey = this.Regconnect(this.hostname, RootKey);

            try
            {
                if (RegOpenKeyEx(RootKey, SubKey, 0, KEY_QUERY_VALUE, ref hKey) != ERROR_NONE)
                {
                    return valList;
                }

                do
                {
                    valLen = 255;
                    valName = new StringBuilder(255);
                    ret = RegEnumValue(hKey, index, valName, ref valLen, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                    if (ret == ERROR_NONE)
                    {
                        valList.Add(valName.ToString());
                    }

                    index += 1;
                }
                while (ret == 0);

                return valList;
            }
            finally
            {
                if (hKey != UIntPtr.Zero)
                {
                    RegCloseKey(hKey);
                }
            }
        }

        public byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }

        public string ByteArrayToString(byte[] arr)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(arr);
        }
        #endregion

        
    }

    
}
