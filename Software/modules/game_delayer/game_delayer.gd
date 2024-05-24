extends MarginContainer
class_name GameDelayer

@export var current_delay_label : Label
var locked : bool = false
var minutes = 0
var seconds = 0

var current_delay : int = 150 : 
	get:
		return current_delay
	set(value):
		current_delay = value if value>0 else 0
		update_ui()

func increase_delay(delay_increase:int):
	if locked:
		return
	current_delay+= delay_increase

func _ready():
	update_ui()

func update_ui():
	minutes = int(current_delay/60)
	seconds = int(current_delay%60)
	current_delay_label.text =  "%02d:%02d"%[minutes,seconds]
	
func start_countdown():
	locked = true
	while(current_delay>0):
		await get_tree().create_timer(1).timeout
		current_delay-= 1
	queue_free()
