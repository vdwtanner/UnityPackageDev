using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FTF.Console.Samples
{
	public class SpawnerSystem : MonoBehaviour, IConsoleSubscriber
	{
		void Start()
		{
			Console.Subscribe(this);
		}

		void OnDestroy()
		{
			Console.Unsubscribe(this);
		}

		public List<CommandFormatDesc> GetCommands()
		{
			List<CommandFormatDesc> commands = new List<CommandFormatDesc>();

			//Build the shared arg descriptors
			CommandArgDesc[] posArgDescs = { new CommandArgDesc("X", "float"),
				new CommandArgDesc("Y", "float"),
				new CommandArgDesc("Z", "float")};

			//add the command descriptors
			commands.Add(new CommandFormatDesc("spawn.cube", posArgDescs));
			commands.Add(new CommandFormatDesc("spawn.sphere", posArgDescs));
			commands.Add(new CommandFormatDesc("spawn.cylinder", posArgDescs));
			return commands;
		}

		public void HandleCommand(Command command)
		{
			if (command.args.Length >= 3)
			{
				if (command.msg.Equals("spawn.cube"))
				{
					Vector3 pos;
					if (ParsePos(command.args, out pos))
					{
						GameObject gob = GameObject.CreatePrimitive(PrimitiveType.Cube);
						gob.transform.position = pos;
					}
				}
				else if (command.msg.Equals("spawn.sphere"))
				{
					Vector3 pos;
					if (ParsePos(command.args, out pos))
					{
						GameObject gob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						gob.transform.position = pos;
					}
				}
				else if (command.msg.Equals("spawn.cylinder"))
				{
					Vector3 pos;
					if (ParsePos(command.args, out pos))
					{
						GameObject gob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
						gob.transform.position = pos;
					}
				}
			}
		}

		bool ParsePos(string[] args, out Vector3 vec)
		{
			float x, y, z;
			bool success = float.TryParse(args[0], out x);
			success &= float.TryParse(args[1], out y);
			success &= float.TryParse(args[2], out z);
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
	}

}
