if DEBUG then
	if Player.Map == nil then
		log("The current map is nil.", true)
	else
		Player.Map.GenerateAnew()
		Player.Map.GenerateWorldFeatures()
		log("The current map has been regenerated.", true)
	end
else
	log("This script can only be executed in debug mode.", false)
end