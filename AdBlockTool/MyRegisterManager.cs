using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdBlockTool
{
    internal class MyRegisterManager
    {
        ////Edge
        private static string _edge_key = "SOFTWARE\\WOW6432Node\\Microsoft\\Edge\\Extensions";
        private static string _edge_update_url = "https://edge.microsoft.com/extensionwebstorebase/v1/crx";
        private static string _edge_policy_key = "SOFTWARE\\Policies\\Microsoft\\Edge\\ExtensionInstallForcelist";

        /// <summary>
        /// chrome
        /// </summary>
        private static string _chrome_key = "SOFTWARE\\WOW6432Node\\Google\\Chrome\\Extensions";
        private static string _chrome_update_url = "https://clients2.google.com/service/update2/crx";
        private static string _chrome_policy_key = "SOFTWARE\\Policies\\Google\\Chrome\\ExtensionInstallForcelist";


        public static void AddKeyToPolicy(string extensionsId, bool isEdge)
        {
            string update_url = isEdge ? _edge_update_url : _chrome_update_url;
            RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey(isEdge ? _edge_policy_key : _chrome_policy_key, true);
            if (registryKey == null)
            {
                registryKey = Registry.LocalMachine.CreateSubKey(isEdge ? _edge_policy_key : _chrome_policy_key, true);
            }

            var names = registryKey.GetValueNames();
            if (names.Length > 0)
            {
                var orderedNames = names.OrderDescending();
                int lastIndex = 0;
                if (Int32.TryParse(orderedNames.Last(), out lastIndex))
                {
                    registryKey.SetValue((lastIndex + 1).ToString(), $"{extensionsId};{update_url}");
                }
            }
            else
            {
                registryKey.SetValue("1", $"{extensionsId};{update_url}");
            }

            registryKey.Close();
        }

        public static void AddKeyToNormalAddress(string extensionsId, bool isEdge)
        {
            string update_url = isEdge ? _edge_update_url : _chrome_update_url;
            RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey(isEdge ? _edge_key : _chrome_key, true);
            if (registryKey != null)
            {
                var extensionKey = registryKey.CreateSubKey(extensionsId);
                extensionKey.SetValue("update_url", update_url);

                registryKey.Close();
            }
        }

        public static void UninstallPolicyKey(string extensionsId, bool isEdge)
        {
            string update_url = isEdge ? _edge_update_url : _chrome_update_url;
            RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey(isEdge ? _edge_policy_key : _chrome_policy_key, true);
            if (registryKey != null)
            {
                var names = registryKey.GetValueNames();
                foreach (var name in names)
                {
                    string? value = registryKey.GetValue(name)?.ToString();
                    if (value != null)
                    {
                        if (value.StartsWith(extensionsId))
                        {
                            registryKey.DeleteValue(name);
                        }
                    }
                }

                registryKey.Close();
            }
        }

        public static void UninstallNormalAddress(string extensionsId, bool isEdge)
        {
            RegistryKey? registryKey = Registry.LocalMachine.OpenSubKey(isEdge ? _edge_key : _chrome_key, true);
            if (registryKey != null)
            {
                var names = registryKey.GetSubKeyNames();
                if (names.Length > 0 && names.Any(key => key.Trim().Equals(extensionsId.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    registryKey.DeleteSubKey(extensionsId);
                }

                registryKey.Close();
            }
        }
    }
}
