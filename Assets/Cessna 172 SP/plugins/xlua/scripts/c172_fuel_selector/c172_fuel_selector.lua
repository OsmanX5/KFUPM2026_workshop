
--
-- THIS SCRIPT CREATE CUSTOM COMMANDS TO CYCLE THE FUEL SELECTOR FROM LEFT, BOTH AND RIGHT (1,4,3)
-- AND REPLACE SHUT-DOWN COMMAND WITH FUEL SELECTION NONE (0)
--
-- THEN HANDLE THE R & L FUEL LEVEL GAUGES, SO THEY WILL SAY ZERO IF NO ELECTRICAL POWER AND ANIMATE SLOWLY
--


----------------------------------- LOCATE AND/OR CREATE DATAREFS -----------------------------------
fuel_tank_selector = find_dataref("sim/cockpit2/fuel/fuel_tank_selector") -- (0=none,1=left,2=center,3=right,4=all)
bus_volts_0 = find_dataref("sim/cockpit2/electrical/bus_volts[0]")
--fuel_quantity_L = find_dataref("sim/cockpit2/fuel/fuel_quantity[0]")
--fuel_quantity_R = find_dataref("sim/cockpit2/fuel/fuel_quantity[1]")
fuel_quantity_L = find_dataref("sim/cockpit2/fuel/fuel_level_indicated_left") --> new, taking pwr or failures in account
fuel_quantity_R = find_dataref("sim/cockpit2/fuel/fuel_level_indicated_right") --> new, taking pwr or failures in account

fuel_tank_selector_c172_handle = create_dataref("laminar/c172/fuel/fuel_tank_selector","number") -- (1=left,2=all,3=right)
fuel_cutoff_selector = create_dataref("laminar/c172/fuel/fuel_cutoff_selector","number") -- (0=none,1=fuel cutoff)
fuel_quantity_c172_L = create_dataref("laminar/c172/fuel/fuel_quantity_L","number")
fuel_quantity_c172_R = create_dataref("laminar/c172/fuel/fuel_quantity_R","number")






------------------------------- FUNCTIONS -------------------------------

-- ANIMATION OF THE FUEL NEEDLES
function update_fuel_needles()
	--**************** OLD CODE REPLACED BY THE NEW FUEL DATAREFS ****************
	--fuel_quantity_c172_L = fuel_quantity_c172_L + ((fuel_quantity_c172_LNEW - fuel_quantity_c172_L) * (5 * SIM_PERIOD))
	--fuel_quantity_c172_R = fuel_quantity_c172_R + ((fuel_quantity_c172_RNEW - fuel_quantity_c172_R) * (5 * SIM_PERIOD))
	--***********************************************
	-- JUST PAIR TO THE NEW FUEL DATAREFS:
	fuel_quantity_c172_L = fuel_quantity_L
	fuel_quantity_c172_R = fuel_quantity_R
end

-- SLOWLY ANIMATE FUNCTION
function func_animate_slowly(reference_value, animated_VALUE, anim_speed)
	animated_VALUE = animated_VALUE + ((reference_value - animated_VALUE) * (anim_speed * SIM_PERIOD))
	return animated_VALUE
end



------------------------------- FUNCTIONS: COMMANDS CALLBACK -------------------------------

function cmd_fuel_selector_up(phase, duration)
	if phase == 0 then
		if (fuel_tank_selector_c172 == 1) then
			fuel_tank_selector_c172 = 4
		elseif (fuel_tank_selector_c172 == 4) then
			fuel_tank_selector_c172 = 3
		elseif (fuel_tank_selector_c172 == 3) then
			fuel_tank_selector_c172 = 3
		else
			fuel_tank_selector_c172 = 4
		end
		if (fuel_cutoff_selector) == 0 then
			fuel_tank_selector = fuel_tank_selector_c172
		end
	end
end

function cmd_fuel_selector_dwn(phase, duration)
	if phase == 0 then
		if (fuel_tank_selector_c172 == 1) then
			fuel_tank_selector_c172 = 1
		elseif (fuel_tank_selector_c172 == 4) then
			fuel_tank_selector_c172 = 1
		elseif (fuel_tank_selector_c172 == 3) then
			fuel_tank_selector_c172 = 4
		else
			fuel_tank_selector = 4
		end
		if (fuel_cutoff_selector) == 0 then
			fuel_tank_selector = fuel_tank_selector_c172
		end
	end
end



function cmd_fuel_cutoff(phase, duration)
	if phase == 0 then
		if (fuel_cutoff_selector == 0) then
			fuel_cutoff_selector = 1
			fuel_tank_selector = 0
		else
			fuel_cutoff_selector = 0
			fuel_tank_selector = fuel_tank_selector_c172
		end
	end
end


------------------------------- LOCATE AND/OR CREATE COMMANDS -------------------------------

-- not used:
-- cmd_fuel_sel_left = find_command("sim/fuel/fuel_selector_lft")
-- cmd_fuel_sel_both = find_command("sim/fuel/fuel_selector_all")
-- cmd_fuel_sel_right = find_command("sim/fuel/fuel_selector_rgt")

cmdcustomfuelup = create_command("laminar/c172/fuel_selector_up","Move the fuel selector up one",cmd_fuel_selector_up)
cmdcustomfueldwn = create_command("laminar/c172/fuel_selector_dwn","Move the fuel selector down one",cmd_fuel_selector_dwn)

cmdfuelshutoff = replace_command("sim/starters/shut_down",cmd_fuel_cutoff)







----------------------------------- RUNTIME CODE -----------------------------------


-- DO THIS EACH FLIGHT START
function flight_start()
	fuel_cutoff_selector = 0
	--fuel_tank_selector = 4
	--fuel_tank_selector_c172 = 4
	fuel_tank_selector_c172 = fuel_tank_selector
	fuel_tank_selector_c172_handle = fuel_tank_selector

end




-- REGULAR RUNTIME
function after_physics()

	-- FUEL NEEDLES
	-- **************** OLD CODE REPLACED BY THE NEW FUEL DATAREFS ****************
	--if (bus_volts_0 > 0.0) then
	--	fuel_quantity_c172_LNEW = fuel_quantity_L
	--	fuel_quantity_c172_RNEW = fuel_quantity_R
	--else
	--	fuel_quantity_c172_LNEW = 0
	--	fuel_quantity_c172_RNEW = 0
	--end
	-- *************************************************
	update_fuel_needles()


	-- KEEP UPDATED THE FUEL SELECTOR VALUE
	if fuel_tank_selector_c172 ~= fuel_tank_selector and fuel_tank_selector ~= 0 then
		fuel_tank_selector_c172 = fuel_tank_selector
	end
	-- KEEP UPDATED THE FUEL HANDLE
	if fuel_tank_selector_c172_handle ~= fuel_tank_selector_c172 then
		new_handle_position = fuel_tank_selector_c172
		if new_handle_position == 4 then new_handle_position = 2 end
		fuel_tank_selector_c172_handle = func_animate_slowly(new_handle_position, fuel_tank_selector_c172_handle, 15)
	end

end


