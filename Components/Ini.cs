using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ChessWPF {
    public class IniFile {
        public string path;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        
        public IniFile(string path) {
            this.path = path;
        }
        public void write(string section, string key, string value) {
            var full = Path.GetFullPath(path);
            if (!full.ToLower().StartsWith(@"C:\windows\system32")) {
                if (!File.Exists(path))
                    File.Create(path).Close();
                WritePrivateProfileString(section, key, value, full);
            }
        }
        public string read(string section, string key) {
            var full = Path.GetFullPath(path);
            if (!full.ToLower().StartsWith(@"C:\windows\system32")) {
                StringBuilder temp = new StringBuilder(255);
                int i = GetPrivateProfileString(section, key, "", temp, 255, full);
                return temp.ToString();
            } else {
                return "";
            }
        }
    }
}