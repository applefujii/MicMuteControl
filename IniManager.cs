using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MicControl
{
    class IniManager
    {
        private String filePath;

        private String full;
        private String[] section;

        public IniManager(String path)
        {
            filePath = path;
            if(File.Exists(@filePath) == false) throw new Exception("iniファイルが存在しません。「"+filePath+"」");

            try
            {
                using (StreamReader st = new StreamReader(@filePath, Encoding.UTF8))
                {
                    // 読み込み
                    full = st.ReadToEnd();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                throw;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

            if (full == null) throw new Exception("iniファイルの読み込みに失敗しました。");
            // セクションごとに分割
            section = Regex.Split(full, @"(?=\[.*\])");
    }

        public String ReadValue( String section, String key )
        {
            foreach ( String s in this.section )
            {
                if(s.StartsWith("["+ section + "]"))
                {
                    Match value = Regex.Match(s, @"(?<=" + key + "=)[^ #\r\n]*");
                    return value.Value;
                }
            }
            return "";
        }

        public int ReadValueInt(String section, String key)
        {
            String val = ReadValue(section, key);
            try
            {
                if (val.StartsWith("0x"))
                {
                    return Convert.ToInt32(val, 16);
                }
                return int.Parse(val);

            }catch (Exception e)
            {
                return 0;
            }
        }

        public bool ReadValueBoolean(String section, String key)
        {
            String val = ReadValue(section, key);
            if (val == "true") return true;
            return false;
        }

        public bool WriteValue(String section, String key, String value )
        {
            String output = "";
            try
            {
                using (StreamWriter st = new StreamWriter(@filePath, false, Encoding.UTF8))
                {
                    bool fSection = false;
                    for (int i=0; i<this.section.Length; i++)
                    {
                        if( this.section[i].StartsWith("[" + section + "]"))
                        {
                            fSection = true;
                            if (Regex.Match(this.section[i], "(?<=[\r\n(\r\n)]" + key + "=)[^#\r\n]*").Success == true)
                            {
                                this.section[i] = Regex.Replace(this.section[i], "(?<=[\r\n(\r\n)]" + key + "=)[^#\r\n]*", value);
                            } else
                            {
                                this.section[i] += key + "=" + value + "\n";
                            }
                        }
                        output += this.section[i];
                    }

                    if(fSection == false)
                    {
                        output += "[" + section + "]\n" + key + "=" + value + "\n";
                    }
                    st.Write(output);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool IsValue(String section, String key)
        {
            foreach (String s in this.section)
            {
                if (s.StartsWith("[" + section + "]"))
                {
                    Match value = Regex.Match(s, @"(?<=" + key + "=)[^ #\r\n]*");
                    return true;
                }
            }
            return false;
        }


        public bool Inherit( String oldIniPath )
        {
            IniManager oldIni = new IniManager(oldIniPath);
            String[] oldSection = oldIni.getSection();

            foreach(String section in oldSection)
            {
                String[] secLine = Regex.Split(section, "\n");
                String secName = "";
                for (int i=0 ; i<secLine.Length; i++)
                {
                    if (secLine[i] == "") continue;
                    if (secName == "")
                    {
                        secName = Regex.Match(secLine[i], @"(?<=\[).+(?=\])").Value;
                        continue;
                    }

                    String[] split = secLine[i].Split('=');
                    String key = split[0];
                    String value = split[1];
                    if (key == "default")
                    {
                        Console.WriteLine(secName, key, value);
                    }
                    WriteValue(secName, key, value);
                }
            }
            return true;
        }

        public String getFull()
        {
            return full;
        }

        public String[] getSection()
        {
            return section;
        }

    }
}
