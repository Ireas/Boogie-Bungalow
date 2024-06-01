extends Node
class_name LoadingScreen

# EXPORTS
@export var animator : AnimationPlayer
@export var background : TextureRect
@export var progress_bar : ProgressBar
@export var message_label : Label
@export var loading_screens : Array[Texture2D]

# VARIABLES
var disabled : bool = false
var is_fading_out : bool = false
var first_time : bool = true

# SIGNALS
signal loading_screen_clicked


func _ready():	
	_set_random_texture()


func set_message(message:String):
	message_label.text = message

func set_percentage(percentage:float):
	progress_bar.value = percentage


# choose a random loading screen backgrond image
func _set_random_texture():
	var selected_texture : Texture2D = loading_screens[RandomNumberGenerator.new().randi_range(0,loading_screens.size()-1)] 
	background.texture = selected_texture


# detect mouse click by user
func _input(event):
	if disabled:
		return
	
	# if user presses left mouse click
	if event is InputEventMouseButton and event.button_index==1:
		disabled = true
		if first_time:
			first_time = false
			animator.play("fade_in")
			await animator.animation_finished
			loading_screen_clicked.emit()
		else: # manual restart, skip open coms
			set_message("Erneuter Versuch...")
			await get_tree().create_timer(4).timeout
			GameManager.request_master_ack()
			

# remove loading screen with cool fade out effect
func delete():
	if is_fading_out:
		return
		
	is_fading_out = true
	progress_bar.value = progress_bar.max_value
	animator.play("fade_out")
	await animator.animation_finished
	
	queue_free()
	
