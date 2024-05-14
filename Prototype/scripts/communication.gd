extends Node

signal send_command(i:int)

func _ready():
	Globals.trigger_communication.connect( func(i): send_command.emit(i) )

