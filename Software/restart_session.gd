extends Button

func _ready():
	GameManager.game_session_finished.connect( func(): visible = true )
	GameManager.pregame_timer_canceled.connect( func(): visible = false )
	button_down.connect(ask_for_restart)

func ask_for_restart():
	var popup : CustomPopup = PopupManager.generate_popup("Rätselstrom bitte einmal aus- und einschalten, damit die Masterantenne enstpannen kann.\nDauer ca. 30s (Software muss nicht geschlossen werden)")
	popup.response_confirm.connect(GameManager.restart_session)
	popup.response_confirm.connect( func() : visible = false )
