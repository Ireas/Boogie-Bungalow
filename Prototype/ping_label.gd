extends Label

func _ready():
	Globals.update_ping.connect(update_ui)

func update_ui(
	ping_4drinks:int, 
	ping_stopptanz:int, 
	ping_sparkasten:int, 
	ping_telefon:int,
	ping_wasserhahn:int,
	ping_sexdungeon:int, 
	ping_schichtplan:int, 
	ping_separee:int
):
	var pings : String = ""
	pings+= "4Drinks: " + str(ping_4drinks) + " | "
	pings+= "Stopptanz: " + str(ping_stopptanz) + " | "
	pings+= "Sparkasten: " + str(ping_sparkasten) + " | "
	pings+= "Telefon: " + str(ping_telefon) + " \n "
	pings+= "Wasserhahn: " + str(ping_wasserhahn) + " | "
	pings+= "Sexdungeon: " + str(ping_sexdungeon) + " | "
	pings+= "Schichtplan: " + str(ping_schichtplan) + " | "
	pings+= "Separee: " + str(ping_separee)
	text = pings
