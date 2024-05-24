extends Panel
class_name ui_button

const BUTTON_DOWNTIME : float = 0.7
signal on_press()

@export var use_button_delay : bool = true

@onready var delay_overlay : Panel = $DelayOverlay
@onready var hover_overlay : Panel = $HoverOverlay
@onready var button_label : Label =  $Label

var hovering : bool = false
var press_during_hover : bool = false


func _ready():
	if use_button_delay:
		EventBus.button_press.connect(temporary_disable)


func _input(event):
	if not hovering:
		return
		
	if event is InputEventMouseButton and event.button_index==1:
		if event.is_pressed():
			press_during_hover = true
		else:
			if(press_during_hover):
				if use_button_delay:
					EventBus.button_press.emit()
				on_press.emit()
			press_during_hover = false


func _on_mouse_entered():
	hovering = true
	hover_overlay.visible = true

func _on_mouse_exited():
	hovering = false
	hover_overlay.visible = false


func temporary_disable():
	delay_overlay.visible = true
	await get_tree().create_timer(BUTTON_DOWNTIME).timeout
	delay_overlay.visible = false
	
