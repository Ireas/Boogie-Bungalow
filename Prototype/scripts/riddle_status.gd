extends PanelContainer

@export var riddle : Globals.Riddle
@export var ping_label : Label
@export var solved_label : Label
@export var state_label : Label


func _ready():
	match(riddle):
		Globals.Riddle.DRINKS:
			Globals.update_status_drinks.connect(update_ui)
		Globals.Riddle.STOPPTANZ:
			Globals.update_status_stopptanz.connect(update_ui)
		Globals.Riddle.SPARKASTEN:
			Globals.update_status_sparkasten.connect(update_ui)
		Globals.Riddle.TELEFON:
			Globals.update_status_telefon.connect(update_ui)
		Globals.Riddle.SEXDUNGEON:
			Globals.update_status_sexdungeon.connect(update_ui)
		Globals.Riddle.SCHICHTPLAN:
			Globals.update_status_schichtplan.connect(update_ui)
		Globals.Riddle.SEPAREE:
			Globals.update_status_separee.connect(update_ui)

func update_ui(ping:int, solved:int, state:int):
	ping_label.text = str(ping)
	solved_label.text = "True" if solved>0 else "False"
	state_label.text = "0%s"%state
