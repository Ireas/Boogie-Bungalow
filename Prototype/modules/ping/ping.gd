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
		Globals.update_status_drinks.connect(set_ping)
	elif(riddle==Globals.Riddle.STOPPTANZ):
		Globals.update_status_stopptanz.connect(set_ping)
	elif(riddle==Globals.Riddle.SPARKASTEN):
		Globals.update_status_sparkasten.connect(set_ping)
	elif(riddle==Globals.Riddle.TELEFON):
		Globals.update_status_telefon.connect(set_ping)
	elif(riddle==Globals.Riddle.SEXDUNGEON):
		Globals.update_status_sexdungeon.connect(set_ping)
	elif(riddle==Globals.Riddle.SCHICHTPLAN):
		Globals.update_status_schichtplan.connect(set_ping)
	elif(riddle==Globals.Riddle.SEPAREE):
		Globals.update_status_separee.connect(set_ping)

func set_ping(ping:int, _solved:int, _state:int):
	ping_label.text = str(ping)
	
	if ping==0:
		texture_frame.texture = texture_no_connection
	elif ping<=500:
		texture_frame.texture = texture_good
	elif ping<=1000:
		texture_frame.texture = texture_medium
	else:
		texture_frame.texture = texture_bad
		
