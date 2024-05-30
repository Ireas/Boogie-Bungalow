extends PanelContainer
class_name RiddleStatus

enum Riddle{
	DRINKS,
	STOPPTANZ,
	SPARKASTEN,
	TELEFON,
	SEXDUNGEON,
	SCHICHTPLAN,
	SEPAREE,
}

@export var riddle : Riddle
@export var ping_label : Label
@export var solved_label : Label
@export var state_label : Label


func _ready():
	match(riddle):
		Riddle.DRINKS:
			EventBus.update_status_drinks.connect(update_ui)
		Riddle.STOPPTANZ:
			EventBus.update_status_stopptanz.connect(update_ui)
		Riddle.SPARKASTEN:
			EventBus.update_status_sparkasten.connect(update_ui)
		Riddle.TELEFON:
			EventBus.update_status_telefon.connect(update_ui)
		Riddle.SEXDUNGEON:
			EventBus.update_status_sexdungeon.connect(update_ui)
		Riddle.SCHICHTPLAN:
			EventBus.update_status_schichtplan.connect(update_ui)
		Riddle.SEPAREE:
			EventBus.update_status_separee.connect(update_ui)


func update_ui(ping:int, solved:int, state:int):
	ping_label.text = str(ping)
	solved_label.text = "True" if solved>0 else "False"
	state_label.text = "%s"%state
