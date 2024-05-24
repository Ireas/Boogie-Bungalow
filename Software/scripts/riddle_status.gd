extends PanelContainer

@export var riddle : Globals.Riddle
@export var ping_label : Label
@export var solved_label : Label
@export var state_label : Label


func _ready():
	match(riddle):
		Globals.Riddle.DRINKS:
			EventBus.update_status_drinks.connect(update_ui)
		Globals.Riddle.STOPPTANZ:
			EventBus.update_status_stopptanz.connect(update_ui)
		Globals.Riddle.SPARKASTEN:
			EventBus.update_status_sparkasten.connect(update_ui)
		Globals.Riddle.TELEFON:
			EventBus.update_status_telefon.connect(update_ui)
		Globals.Riddle.SEXDUNGEON:
			EventBus.update_status_sexdungeon.connect(update_ui)
		Globals.Riddle.SCHICHTPLAN:
			EventBus.update_status_schichtplan.connect(update_ui)
		Globals.Riddle.SEPAREE:
			EventBus.update_status_separee.connect(update_ui)

func update_ui(ping:int, solved:int, state:int):
	ping_label.text = str(ping)
	solved_label.text = "True" if solved>0 else "False"
	state_label.text = "%s"%state
