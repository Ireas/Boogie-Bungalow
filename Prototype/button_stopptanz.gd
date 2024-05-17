extends Button

@export var command : command_type

enum command_type{
	STOP,
	DANCE,
}

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.




# Called every frame. 'delta' is the elapsed time since the previous frame.
func do_command():
	if command==command_type.STOP:
		Globals.music_pause.emit()
	else:
		Globals.music_continue.emit()
