extends Control

@export var riddle : Globals.Riddle


@export var ping_label : Label
@export var texture_frame : TextureRect

@export var texture_good : Texture
@export var texture_medium : Texture
@export var texture_bad : Texture
@export var texture_no_connection : Texture


func _ready():
	if(riddle==Globals.Riddle.DRINKS):
		EventBus.update_status_drinks.connect(set_ping)
	elif(riddle==Globals.Riddle.STOPPTANZ):
		EventBus.update_status_stopptanz.connect(set_ping)
	elif(riddle==Globals.Riddle.SPARKASTEN):
		EventBus.update_status_sparkasten.connect(set_ping)
	elif(riddle==Globals.Riddle.TELEFON):
		EventBus.update_status_telefon.connect(set_ping)
	elif(riddle==Globals.Riddle.SEXDUNGEON):
		EventBus.update_status_sexdungeon.connect(set_ping)
	elif(riddle==Globals.Riddle.SCHICHTPLAN):
		EventBus.update_status_schichtplan.connect(set_ping)
	elif(riddle==Globals.Riddle.SEPAREE):
		EventBus.update_status_separee.connect(set_ping)

func set_ping(ping:int, _solved:int, _state:int):
	ping_label.text = str(ping)
	
	if ping<=0:
		texture_frame.texture = texture_no_connection
	elif ping<=600:
		texture_frame.texture = texture_good
	elif ping<=1000:
		texture_frame.texture = texture_medium
	else:
		texture_frame.texture = texture_bad
		
