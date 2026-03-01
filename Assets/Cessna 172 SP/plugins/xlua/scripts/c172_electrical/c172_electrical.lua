
----------------------------------------------------------------------------------------------------------
-- THIS SCRIPT ANIMATE SLOWLY THE CUSTOM AMP GAUGE;
-- PLUS, IT AUTOMATICALLY SWITCH ON/OFF THE CROSS TIE IN THE CESSNA G1000 DEPENDING ON BUS VOLTS (RELAY);
-- AND THEN, IT HANDLE THE ALT/BAT MASTER SWITCHES LOGIC (mechanically interlocked in the real Cessna).
----------------------------------------------------------------------------------------------------------




----------------------------------- LOCATE AND/OR CREATE DATAREFS -----------------------------------

battery_amps = find_dataref("sim/cockpit2/electrical/battery_amps[0]")
main_bus_volts = find_dataref("sim/cockpit2/electrical/bus_volts[0]")
max_bat_volt_standard = find_dataref("sim/aircraft/limits/max_bat_volt_standard") -- the nominal battery volts from acf planemaker
cross_tie = find_dataref("sim/cockpit2/electrical/cross_tie")
generator_on = find_dataref("sim/cockpit2/electrical/generator_on[0]")
battery_on = find_dataref("sim/cockpit2/electrical/battery_on[0]")
num_batteries = find_dataref("sim/aircraft/electrical/num_batteries") -- we use this to know whether we are on the G1000 or analogue cessna

battery_amps_c172 = create_dataref("laminar/c172/electrical/battery_amps","number") -- the instrument needle








---------------------------------------- FUNCTIONS ---------------------------------------

-- ANIMATION OF THE AMPS NEEDLE
function update_amps_needles()
	battery_amps_c172 = battery_amps_c172 + ((battery_amps_c172NEW - battery_amps_c172) * (10 * SIM_PERIOD))
end







----------------------------------- RUNTIME CODE -----------------------------------


-- DO THIS EACH FLIGHT START
--function flight_start()
	--none
--end




-- REGULAR RUNTIME
function after_physics()

	-- UPDATE AMPS NEEDLE
	battery_amps_c172NEW = battery_amps
	update_amps_needles()


	-- CROSS-TIE RELAY ON THE G1000 CESSNA
	if num_batteries == 2 then
		if main_bus_volts > max_bat_volt_standard then cross_tie = 1 else cross_tie = 0 end
	end


	-- ALT/BAT MASTER SWITCHES LOGIC
	if batteryPREV ~= battery_on then
		if battery_on == 0 then generator_on = 0 end
	end
	if generatorPREV ~= generator_on then
		if generator_on == 1 then battery_on = 1 end
	end
	batteryPREV = battery_on
	generatorPREV = generator_on

end


