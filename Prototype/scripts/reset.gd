extends VBoxContainer

signal on_soft_reset()
signal on_hard_reset()

func _ready():
	EventBus.session_start.connect( func(): visible=true )

func soft_reset():
	var popup : CustomPopup = PopupManager.generate_popup("Durch einen Soft Reset wird das Netzwerk zurückgesetzt. Schwache Verbindungen können so wiederhergestellt werden. \nDauer ca. 10-20s (Währenddessen ist keine Bedienung möglich! Verbindet sich danach wieder automatisch.)")
	popup.response_confirm.connect( func(): on_soft_reset.emit() )

func hard_reset():
	var popup : CustomPopup = PopupManager.generate_popup("Durch einen Hard Reset wird das komplette Netzwerk zurückgesetzt. Davor sollte der Soft Reset probiert werden. \nDauer ca. 30s (Währenddessen ist keine Bedienung möglich! Verbindet sich danach wieder automatisch.)")
	popup.response_confirm.connect( func(): on_hard_reset.emit() )
