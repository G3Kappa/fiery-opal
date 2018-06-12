if DEBUG then
	rect(-10, -10, 20, 20, "rockwall")
	rect(-9, -9, 18, 18, "dirt")
	spawn(0, 0, "Humanoid", 100)
	store("Freezzino", 1)
	equip("Freezzino")

	for i=0,100 do
		Player.Brain.Attack()
		Player.Brain.Turn(math.pi / 4)
		Sleep(16)
	end
else
	log("This script can only be executed in debug mode.", false)
end