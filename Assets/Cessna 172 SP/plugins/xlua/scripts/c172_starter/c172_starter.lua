
--
-- THIS SCRIPT CREATE THE CUSTOM COMMANDS TO HANDLE THE OFF/R/L/BOTH/START IGNITION KEY
--





-- LOCATE DATAREFS AND COMMANDS

simDR_ignition_pos = find_dataref("sim/cockpit2/engine/actuators/ignition_key[0]")
simCMD_ignition_down_1 = find_command("sim/magnetos/magnetos_down_1")
simCMD_ignition_up_1 = find_command("sim/magnetos/magnetos_up_1")
simCMD_engage_starter = find_command("sim/starters/engage_starter_1")





-- CREATE FUNCTIONS

function C172_ignition_down_CMDhandler(phase, duration)
	if phase == 0 then
		if simDR_ignition_pos ~= 4 then
			simCMD_ignition_down_1:once()
		end
	end
end

function C172_ignition_up_CMDhandler(phase, duration)
	if phase == 0 then
		-- We have to include the "4" position because trying to INCREASE the command while turning the 
		-- key will cause us to end the previous up command which stops the starter, but then in the same
		-- frame runs START on the same command all over again which hasn't given our ignition_pos time
		-- to fall back to 3 yet...
		if simDR_ignition_pos == 3 or simDR_ignition_pos == 4 then
			simCMD_engage_starter:start()
		elseif simDR_ignition_pos < 3 then
			simCMD_ignition_up_1:once()
		end
	elseif phase == 2 then
		print("Release....")
		if simDR_ignition_pos == 4 then
			print("STOPPING COMMAND!")
			simCMD_engage_starter:stop()
		end
	end
end




-- CREATE COMMANDS

C172CMD_ignition_down = create_command("laminar/c172/ignition_down","Ignition Sel Down",C172_ignition_down_CMDhandler)
C172CMD_ignition_up = create_command("laminar/c172/ignition_up"  ,"Ignition Sel Up",C172_ignition_up_CMDhandler)