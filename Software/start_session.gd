extends Button

func _ready():
	GameManager.new_game_session_started.connect(setup)
	GameManager.pregame_timer_started.connect( func(): visible = false )
	GameManager.pregame_timer_canceled.connect( func(): visible = true )

func setup():
	visible = true
