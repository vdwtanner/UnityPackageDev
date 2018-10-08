Version: 1.5

=========COMMAND BUS=========

Subscribing to the comand bus:
	*The class must implement IConsoleSubscriber.
	*You must call Console.Subscribe(IConsoleSubscriber) and pass your object in order to receive commands
	*HandleCommand(Command) is where you will receive commands from the bus. Listen for specific commands here.
	*GetCommands() should return a list of Command Format Descriptors so that users can get suggestions for your commands
	*NOTE: Don't forget to unsubscribe from the command bus if the object is ever destroyed!
	
Unsubscribing from the command bus:
	*You must call Console.Unsubscribe(IConsoleSubscriber) and pass your object reference to unsubscribe.
	*Failing to unsubscribe before destroying the object will cause a null pointer ref the next time a command is sent on the bus

Sending messages on the command bus:
	*NOTE: not recommended for general use since commands are sent to all subscribers and is therefore inefficient
	*Console.PostCommand(Command) allows you to programatically send messages on the command bus

=========CONSOLE=========

To view a listing of all commands:
	use the command "help"
	you can filter the listing by passing a filter string to the "help" command
	
Console logging:
	Log(object)
		forwards to Debug.Log() and logs to in game console
	Log(object, color)
		forwards to Debug.Log() and logs to in game console with the specified color
	Warn(object)
		forwards to Debug.LogWarning() and logs to in game console as yellow text
	Error(object)
		forwards to Debug.LogError() and logs to in game console as red text
	Verbose(object)
		If console.verbose is true, forwards to Log(object)
	Verbose(object, color)
		If console.verbose is true, forwards to Log(object, color)
		
Show/hide console:
	Press `
	You can listen for console activation / deactivation by calling Console.SubscribeToConsoleActivationEvent(ConsoleActivationListener)
		There is also an unsubscribe method that should be called when the listening object is destroyed
	
To select an autocomplete option:
	press PageUp or PageDown to move the selector
	press TAB to autocomplete

To enter a string argument that includes spaces, wrap it in quotes

To submit a command in the console:
	press enter

=========SELECTION=========

To scroll through the selectionInfo window, use CTRL + PageUp and CTRL + PageDown

To add selectionInfo to scripts:
	implement ISelectionInfo interface

	=======AFTER SELECTING OBJECT=======
	*selection.selectComponent <typeName>
		- Select a component on this object or one of its parents
		- Brings up an inspector view of that component
	*selection.gameObject
		- Select the GameObject this component is attached too
		- Must have already selected a component
	*selection.detailed
		- See an inspector view of every MonoBehaviour attached to the selected
			GameObject
		- If a component is currently selected, this will select the GameObject
			it is attached to as well

=========MACROS=========

To add macros, put a macro_listing.xml file in Streamingassets in the root Assets directory.
There is a sample macro_listing.xml in the Samples folder of the package.

All macros must have a unique commandName attribute.
The macro handler will attempt to validate macros on load.
The macro handler only exists if the console GUI is loaded.
The text value of an <arg> is the default value.
Args with default values can only exist after args without default values.
Arg values replace the params indicated in the command strings with %[arg-id].
Format is as follows:
	<MacroListing>
		<macros>
			<macro keyName="h" commandName="test">
				<args>
					<arg type="string" id="filter" name="help_filter">macro</arg>
					<arg type="float" id="x" name="X">1</arg>
					<arg type="float" id="y" name="Y">1</arg>
					<arg type="float" id="z" name="Z">1</arg>
				</args>
				<command>help %filter</command>
				<command>spawn.cube %x %y %z</command>
			</macro>
		</macros>
	</MacroListing>