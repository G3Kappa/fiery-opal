if DEBUG then
	startsv(5000, 2)
	connect("127.0.0.1", 5000, "Kappa")
else
	log("This script can only be executed in debug mode.", false)
end