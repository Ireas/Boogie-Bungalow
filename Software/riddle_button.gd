extends Button

# CONSTANTS
const BUTTON_DOWNTIME : float = 0.7

# EXPORT
@export var ignore_delay : bool = false
@onready var delay_overlay : Panel = $DisableOverlay


func _ready():
	if not ignore_delay:
		EventBus.button_press.connect(temporary_disable)
		button_down.connect( func(): EventBus.button_press.emit() )

func temporary_disable():
	delay_overlay.visible = true
	await get_tree().create_timer(BUTTON_DOWNTIME).timeout
	delay_overlay.visible = false
	
