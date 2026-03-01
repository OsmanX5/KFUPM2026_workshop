
-- THIS SCRIPT IS HERE TO INITIALIZE SOME VALUES



----------------------------------- LOCATE DATAREFS OR COMMANDS -----------------------------------
startup_running = find_dataref("sim/operation/prefs/startup_running") -- start as cold and dark or not
batteryEMERG = find_dataref("sim/cockpit2/electrical/battery_on[1]")
crossTIE = find_dataref("sim/cockpit2/electrical/cross_tie")
fuel_pump_on = find_dataref("sim/cockpit2/engine/actuators/fuel_pump_on[0]")
num_batteries = find_dataref("sim/aircraft/electrical/num_batteries") -- we use this to know whether we are on the G1000 or analogue cessna
panel_glareshield_brightness = find_dataref("sim/cockpit2/switches/panel_brightness_ratio[3]")
instruments_brightness = find_dataref("sim/cockpit2/electrical/instrument_brightness_ratio_manual[0]")
autoboard_in_progress = find_dataref("sim/flightmodel2/misc/auto_board_in_progress")
autostart_in_progress = find_dataref("sim/flightmodel2/misc/auto_start_in_progress")

interior_lites_0 = find_dataref("sim/cockpit2/switches/instrument_brightness_ratio[0]")
interior_lites_1 = find_dataref("sim/cockpit2/switches/instrument_brightness_ratio[1]")
interior_lites_2 = find_dataref("sim/cockpit2/switches/instrument_brightness_ratio[2]")
interior_lites_3 = find_dataref("sim/cockpit2/switches/instrument_brightness_ratio[3]")


--------------------------------- CREATING FUNCTIONS TO CALL BACK ---------------------------------

-- NONE

function aircraft_load ()
	print('Deferred flight start')
	interior_lites_0 = 0.4
	interior_lites_1 = 0.75
	interior_lites_2 = 0.75
	interior_lites_3 = 0.2
	panel_glareshield_brightness = 0.3
end

--------------------------------- DO THIS EACH FLIGHT START ---------------------------------
function flight_start()

-- IF NUMBERS OF BATTERIES IS 1 MEAN WE ARE ON THE ANALOG CESSNA (SINCE THE G1000 CESSNA HAS TWO)

	if startup_running == 1 then -------- IF START WITH ENGINE RUNNING
		if num_batteries == 1 then -- IN THE ANALOG CESSNA:
			crossTIE = 1
			fuel_pump_on = 0
		elseif num_batteries == 2 then -- IN THE G1000 CESSNA:
			crossTIE = 0
			batteryEMERG = 1
			fuel_pump_on = 0
		end
	else -------------------------------- IF START COLD AND DARK:
		crossTIE = 0
		batteryEMERG = 0
		fuel_pump_on = 0
	end

end




--------------------------------- REGULAR RUNTIME ---------------------------------
function after_physics()

	--------------------------
	-- AUTOBOARD / AUTOSTART: TURN ON ALSO THE CROSS-TIE (avionic bus2) IF WE ARE ON THE ANALOG CESSNA
	--------------------------
	if (autoboard_in_progress + autostart_in_progress > 0) and (num_batteries == 1) then
		crossTIE = 1
	end
	
	--------------------------
	-- KEEP SYNC GLARESHIELD_BRIGHTNESS WITH INSTRUMENT_BRIGHTNESS IF WE ARE ON THE G1000 CESSNA
	--------------------------
	if (num_batteries == 2) then
		panel_glareshield_brightness = instruments_brightness
	end

end