using System;
using System.Configuration;
using System.Diagnostics;

namespace ProductionSecurityControlSystem.ConfigHelper
{
    class ConfigHelper
    {
        /// <summary>
        ///     配置工具提供以下四个静态方法：
        ///     1，GetSoftConfig，接受一个string作为参数，返回App.Config文件对应字段的信息
        ///     2，SetSoftConfig,接受两个string参数（字段名，值），将设定存入App.Config并refreshion,成功返回true
        ///     3，GetDbConfig，返回数据库连接字
        ///     4，SetDbConfig，接受两个stirng参数（数据连接串名称，连接字符串）设定数据库连接字，成功返回true
        /// </summary>
        public class SoftConfig
        {
            #region SetSoftConfig方法

            /// <summary>
            ///     SetSoftConfig
            /// </summary>
            /// <param name="key">字段名</param>
            /// <param name="value">值</param>
            /// <returns>bool（true/false）</returns>
            public static bool SetSoftConfig(string key, string value)
            {
                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    if (config.AppSettings.Settings[key] != null)
                        config.AppSettings.Settings[key].Value = value;
                    else
                        config.AppSettings.Settings.Add(key, value);
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("appSettings");
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            #endregion

            #region GetSoftConfig方法

            /// <summary>
            ///     GetSoftConfig
            /// </summary>
            /// <param name="key">字段名</param>
            /// <returns>对应值/没有对应值得情况下返回空字符串</returns>
            public static string GetSoftConfig(string key)
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                    return config.AppSettings.Settings[key].Value;
                return string.Empty;
            }

            #endregion

            #region GetDbConfig方法

            /// <summary>
            ///     GetDbConfig
            /// </summary>
            /// <returns>数据库连接字/空字符串</returns>
            public static string GetDbConfig(string settingName = "FittingSystemDbSettings")
            {
                var settings = ConfigurationManager.ConnectionStrings[settingName];
                string test = settings.ConnectionString;
                try
                {
                    if (string.IsNullOrEmpty(settings.ConnectionString))
                        return string.Empty;
                    return settings.ConnectionString;
                }
                catch(Exception error)
                {
                    Debug.Print(error.Message);
                    return string.Empty;
                }
            }

            #endregion

            #region SetDbConfig方法

            /// <summary>
            ///     SetDbConfig
            /// </summary>
            /// <param name="connStr">Db连接字符串</param>
            /// <param name="settingName">连接字符串设定名称,默认为FittingSystemDbSettings</param>
            /// <returns>修改成功返回true，否则返回false</returns>
            public static bool SetDbConfig(string connStr, string settingName = "FittingSystemDbSettings")
            {
                try
                {
                    var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var conSection = (ConnectionStringsSection)config.GetSection("connectionStrings");
                    if (conSection.ConnectionStrings[settingName] != null) //有对应的连接字符串就修改
                    {
                        conSection.ConnectionStrings[settingName].ConnectionString = connStr;
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("connectionStrings");
                    }
                    else //没有就新增
                    {
                        var conSettings = new ConnectionStringSettings(settingName, connStr, "System.Data.SqlClient");
                        conSection.ConnectionStrings.Add(conSettings);
                        config.Save(ConfigurationSaveMode.Full);
                        ConfigurationManager.RefreshSection("connnectionStrings");
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            #endregion
        }
    }
}
