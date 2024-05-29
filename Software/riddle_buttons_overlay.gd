extends Control

func _ready():
	GameManager.new_game_session_started.connect(setup)
	GameManager.game_session_started.connect( func(): visible = false )

func setup():
	visible = true
