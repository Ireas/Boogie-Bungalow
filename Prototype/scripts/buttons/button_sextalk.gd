extends Button

enum Task{
	ENABLE,
	OPEN,
}

@export var task : Task


func _ready():
	## execute chosen task when button is pressed
	button_down.connect(ExecuteMyTask)


func ExecuteMyTask():
	## send command to c# script
	Globals.trigger_communication.emit(40 + int(task))
