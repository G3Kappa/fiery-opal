if Player.Map == nil then
	log("The current map is nil.", true)
else
	local pf = RoomComplexPrefab.__new(Player.LocalPosition)
	pf.Place(Player.Map, nil)
end