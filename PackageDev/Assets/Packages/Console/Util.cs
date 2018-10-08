using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FTF.Console
{
	#region structs
	public struct Command
	{
		/// <summary>The actual command message, EX: "director.spawnLimit"</summary>
		public string msg;
		/// <summary>A set of arguments that accompany the message. may be 0 or more depending on the command</summary>
		public string[] args;

		public Command(string msg, string[] args)
		{
			this.msg = msg;
			this.args = args;
		}

		public override string ToString()
		{
			string output = "Command: [" + msg + ", (";
			for (int i = 0; i < args.Length; i++)
			{
				if (i > 0)
					output += ", ";
				output += args[i];
			}
			output += ")]";

			return output;
		}
	}

	public struct CommandFormatDesc
	{
		/// <summary>The msg that will be listened for</summary>
		public string msg;

		/// <summary>A brief description / manual of what this command will do</summary>
		public string desc;
		/// <summary>An array of argument descriptions that will be used to provide context to the user</summary>
		public CommandArgDesc[] argDescs;

		public CommandFormatDesc(string msg)
		{
			this.msg = msg;
			this.desc = "";
			argDescs = new CommandArgDesc[0];
		}

		public CommandFormatDesc(string msg, string manPage)
		{
			this.msg = msg;
			this.desc = manPage;
			argDescs = new CommandArgDesc[0];
		}

		public CommandFormatDesc(string msg, CommandArgDesc[] argDescs)
		{
			this.msg = msg;
			this.desc = "";
			this.argDescs = argDescs;
		}

		public CommandFormatDesc(string msg, CommandArgDesc[] argDescs, string desc)
		{
			this.msg = msg;
			this.desc = desc;
			this.argDescs = argDescs;
		}

		public override string ToString()
		{
			string output = msg;
			for (int i = 0; i < argDescs.Length; i++)
			{
				output += " " + argDescs[i].ToString();
			}
			return output;
		}

		public string GetManPage()
		{
			string manPage = "<b><color=#0F8FC0>" + msg + "</color></b>\n";
			manPage += desc;
			for(int i=0; i<argDescs.Length; i++)
			{
				manPage += "\n@ " + argDescs[i].ToString() + " - " + argDescs[i].desc;
			}

			return manPage;
		}
	}

	public struct CommandArgDesc
	{
		/// <summary>Name of the argument. Used to provide context to the user</summary>
		public string name;
		/// <summary>The argument type (string, int, float, bool)</summary>
		public string type;
		/// <summary>Is this argument optional? Should be the last in the list of args.</summary>
		public bool optional;

		/// <summary>A brief, one line description of the argument </summary>
		public string desc;

		/// <param name="name">Arg name</param>
		/// <param name="type">Arg type</param>
		/// <param name="desc">A brief, one line description of the argument</param>
		public CommandArgDesc(string name, string type, bool optional = false)
		{
			this.name = name;
			this.type = type;
			this.optional = optional;
			desc = "";
		}

		/// <param name="name">Arg name</param>
		/// <param name="type">Arg type</param>
		/// <param name="desc">A brief, one line description of the argument</param>
		/// <param name="optional"></param>
		public CommandArgDesc(string name, string type, string desc, bool optional = false)
		{
			this.name = name;
			this.type = type;
			this.optional = optional;
			this.desc = desc;
		}

		public override string ToString()
		{
			string output = "";
			if (optional)
				output += "[";
			output += type + ":" + name;
			if (optional)
				output += "]";
			return output;
		}
	}
	#endregion

	#region interfaces
	public interface IConsoleSubscriber
	{
		/// <summary>
		/// This function will be called by the console bus with the command given by the user
		/// </summary>
		/// <param name="command">The comand given by the user. It may or may not be relevant to this subscriber.</param>
		void HandleCommand(Command command);

		/// <summary>
		/// Get the list of commands that are accepted by this subscriber.
		/// The list will be used to generate contextual suggestions.
		/// </summary>
		/// <returns>The list of commands that this subscriber cares about</returns>
		List<CommandFormatDesc> GetCommands();
	}
	#endregion

	public class Util
	{
		/// <summary>
		/// Helper function to parse vector 3 from string args
		/// </summary>
		/// <param name="args">the args from a command</param>
		/// <param name="vec">The vector3 to be returned. Will be Vector3.zero if this method fails</param>
		/// <param name="startingIndex">the starting index</param>
		/// <returns>True if we were able to parse the args, else false</returns>
		public static bool TryParseVector3(string[] args, out Vector3 vec, int startingIndex = 0)
		{
			float x, y, z;
			if (args.Length <= startingIndex + 2)
			{
				vec = Vector3.zero;
				return false;
			}

			bool success = float.TryParse(args[startingIndex], out x);
			success &= float.TryParse(args[startingIndex + 1], out y);
			success &= float.TryParse(args[startingIndex + 2], out z);
			if (success)
			{
				vec = new Vector3(x, y, z);
			}
			else
			{
				vec = Vector3.zero;
			}

			return success;
		}

		/// <summary>
		/// Generates a very crude inspector from a component.
		/// Super expensive to do this
		/// </summary>
		/// <param name="component"></param>
		/// <returns></returns>
		public static string GenerateReflectionInspector(Component component, string headerHexColor = "#F0F000", string rowAccentHexColor = "#000000")
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();

			System.Type comType = component.GetType();

			//Public Fields
			var fields = comType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField);
			if (fields.Length > 0)
			{
				builder.AppendFormat("<color={0}><b>=====PUBLIC FIELDS=====</b></color>\n", headerHexColor);
				for (int i = 0; i < fields.Length; i++)
				{
					if (i % 2 == 0)
						builder.AppendFormat("{0,-20} {1,23}\n", NicifyVariableName(fields[i].Name, 20), fields[i].GetValue(component));
					else
						builder.AppendFormat("<color={2}>{0,-20} {1,23}</color>\n", NicifyVariableName(fields[i].Name, 20), fields[i].GetValue(component), rowAccentHexColor);
				}
			}

			var properties = comType.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance);
			if (properties.Length > 0)
			{
				builder.AppendFormat("\n<color={0}><b>=====PROPERTIES=====</b></color>\n", headerHexColor);
				for (int i = 0; i < fields.Length; i++)
				{
					try
					{
						if (i % 2 == 0)
							builder.AppendFormat("{0,-20} {1,23}\n", NicifyVariableName(properties[i].Name, 20), properties[i].GetValue(component, null));
						else
							builder.AppendFormat("<color={2}>{0,-20} {1,23}</color>\n", NicifyVariableName(properties[i].Name, 20), properties[i].GetValue(component, null), rowAccentHexColor);
					}
					catch
					{
						//unable to get value of property
					}

				}
			}

			//Private Fields
			fields = comType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (fields.Length > 0)
			{
				builder.AppendFormat("\n<color={0}><b>=====PRIVATE FIELDS=====</b></color>\n", headerHexColor);
				for (int i = 0; i < fields.Length; i++)
				{
					if (i % 2 == 0)
						builder.AppendFormat("{0,-20} {1,23}\n", NicifyVariableName(fields[i].Name, 20), fields[i].GetValue(component));
					else
						builder.AppendFormat("<color={2}>{0,-20} {1,23}</color>\n", NicifyVariableName(fields[i].Name, 20), fields[i].GetValue(component), rowAccentHexColor);
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Pretty print names of variables
		/// </summary>
		/// <param name="name">A variable name</param>
		/// <param name="maxLen">The maximum length that the formatted string should be</param>
		/// <returns>Spaces between camel case and capital letters</returns>
		public static string NicifyVariableName(string name, int maxLen = -1, bool ellipsize = true)
		{
			name = name[0].ToString().ToUpper() + name.Substring(1);
			name = name.Replace("_", " ");
			var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

			name = r.Replace(name, " ");
			if (maxLen == -1)
				return name;
			if (name.Length > maxLen)
			{
				if (ellipsize && maxLen > 3)
					name = name.Substring(0, maxLen - 3) + "...";
				else
					name = name.Substring(0, maxLen);
			}
			return name;
		}
	}
}