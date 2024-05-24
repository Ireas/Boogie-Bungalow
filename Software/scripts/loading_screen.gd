extends Node

@export var animator : AnimationPlayer
@export var background_frame : TextureRect
@export var progress_bar : ProgressBar
@export var label_connection : Label
@export var skip_sync : bool = false
@export var loading_screens : Array[Texture2D]


var disabled : bool = false
var block_finish : bool = false

signal send_sync_request_to_hardware()

func _ready():
	if skip_sync:
		queue_free()
		return
	
	# choose a random loading screen backgrond image
	var selected_texture : Texture2D = loading_screens[RandomNumberGenerator.new().randi_range(0,loading_screens.size()-1)] 
	background_frame.texture = selected_texture
	
	EventBus.sync_package_send.connect( func(): progress_bar.value+= 1 )
	EventBus.sync_successful.connect( finish_connecting )
	EventBus.sync_restart.connect( restart_connecting )
	EventBus.ack_timeout.connect( hint_electricity )

func _input(event):
	if disabled:
		return
	
	if event is InputEventMouseButton and event.button_index==1:
		disabled = true
		animator.play("fade_in")
		EventBus.sync_start.emit()
		send_sync_request_to_hardware.emit()
	

func finish_connecting():
	if block_finish:
		return
		
	block_finish = true
	progress_bar.value = progress_bar.max_value
	animator.play("fade_out")
	await animator.animation_finished
	
	queue_free()

func hint_electricity():
	label_connection.text = "Hardware reagiert nicht. Ist der Rätselstrom angeschalten?"

func restart_connecting():
	if block_finish:
		return
		
	progress_bar.value = 0
	label_connection.text = "Synchronisation fehlgeschlagen. Erneuter Versuch..."
	
