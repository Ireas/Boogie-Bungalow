extends MarginContainer
class_name GameDelayer

@export var current_delay_label : Label
var locked : bool = false

var current_delay : int = 90 : 
	get:
		return current_delay
	set(value):
		current_delay = value if value>0 else 0
		update_ui()

func set_delay(new_delay:int):
	if locked:
		return
	current_delay = new_delay

func increase_delay(delay_increase:int):
	if locked:
		return
	current_delay+= delay_increase

func update_ui():
	current_delay_label.text = str(current_delay) + "s" 
	
func start_countdown():
	locked = true
	while(current_delay>0):
		await get_tree().create_timer(1).timeout
		current_delay-= 1
	queue_free()
