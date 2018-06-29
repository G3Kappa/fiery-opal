if Player.Map == nil then
	log("The current map is nil.", true)
else
	Player.Map.GenerateAnew()
	Player.Map.GenerateWorldFeatures()
	log("The current map has been regenerated.", true)
end