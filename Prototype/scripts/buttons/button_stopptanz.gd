extends Button

enum Task{
	INITIALIZE,
	DANCE,
	STOP,
	SOLVE,
}

@export var task : Task


func _ready():
	## execute chosen task when button is pressed
	button_down.connect(ExecuteMyTask)
	
	## disable all stopptanz buttons when solved
	Globals.stopptanz_solved.connect(Disable) 
	
	## disable all buttons but initialize at start 
	if(task!=Task.INITIALIZE):
		Disable()
		Globals.stopptanz_initialized.connect(Enable)


func ExecuteMyTask():
	## send command to c# script
	Globals.trigger_communication.emit(20 + int(task))
	
	## execute custom task
	match task:
		Task.INITIALIZE: # enable buttons
			Globals.music_play_track.emit("stopptanz")
			Globals.stopptanz_initialized.emit()
			Disable()
		Task.DANCE: # continue music
			Globals.music_continue.emit()
		Task.STOP: # pause music
			Globals.music_pause.emit()
		Task.SOLVE: # disable buttons
			Globals.stopptanz_solved.emit()
			Globals.music_play_track.emit("atmo2")


func Enable():
	## enable button if disabled
	if(!disabled):
		return
	disabled = false;
	self_modulate = Color(1,1,1,1)

func Disable():
	## disable button
	disabled = true;
	self_modulate = Color(0.8,0.8,0.8,1)
