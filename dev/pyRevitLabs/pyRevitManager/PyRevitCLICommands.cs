using System.Text;
using System.Collections.Generic;

using pyRevitManager.Properties;

using YamlDotNet.Serialization;

namespace pyRevitManager {
    class PyRevitCLICommands {

        // return command definitions from resource file
        // the resource defines the command structure of the cli process
        // parsing and generating the help depends on this definition
        public static string CommandsDefinition {
            get {
                return Encoding.UTF8.GetString(Resources.pyrevit);
            }
        }

        // process command definitions from resource file and create a dictionary
        public static object Commands {
            get {
                var deserializer = new Deserializer();
                return deserializer.Deserialize(CommandsDefinition, typeof(Dictionary<string, object>));
            }
        }
    }
}
