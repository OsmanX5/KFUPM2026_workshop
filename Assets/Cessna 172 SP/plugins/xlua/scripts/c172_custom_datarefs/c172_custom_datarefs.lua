
---------------------------------------------------------------
-- THIS SCRIPT IS HERE TO CREATE SOME CUSTOM WRITABLE DATAREFS
-- AND CONTROL THEIRS BEHAVIOR
---------------------------------------------------------------


------------------------------- FUNCTIONS: FOR WRITABLE DATAREFS -------------------------------

function knobTASfunc()
	-- do nothing
end


function knobEGTfunc()
	-- do nothing
end

function knobOATfunc()
	-- toggle OAT from F and C when in temp mode
	if knob_OAT < 2 then cmd_thermo_units_toggle:once() end
end

function flapsideSHIFTfunc()
	-- do nothing
end






----------------------------------- LOCATE AND/OR CREATE DATAREFS AND COMMANDS -----------------------------------

adf1_frequency_hz = find_dataref("sim/cockpit2/radios/actuators/adf1_frequency_hz")
adf1_standby_frequency_hz = find_dataref("sim/cockpit2/radios/actuators/adf1_standby_frequency_hz")
flap_ratio = find_dataref("sim/cockpit2/controls/flap_handle_request_ratio")
OAT_is_metric = find_dataref("sim/cockpit2/temperature/outside_air_temp_is_metric")

knob_TAS = create_dataref("laminar/c172/knob_TAS","number",knobTASfunc) -- the airspeed ruler
knob_EGT = create_dataref("laminar/c172/knob_EGT","number",knobEGTfunc) -- the EGT red needle
knob_OAT = create_dataref("laminar/c172/knob_OAT","number",knobOATfunc) -- the OAT/VOLTS toggle button of the clock: 0 is F / 1 is C / 2 is volts
flap_handle_side_shift = create_dataref("laminar/c172/flap_side_shift","number",flapsideSHIFTfunc) -- the side shift of the flap handle


cmd_thermo_units_toggle = find_command("sim/instruments/thermo_units_toggle") -- toggle OAT from F and C




----------------------------------- RUNTIME CODE -----------------------------------

function flight_start()
	flap_handle_side_shift = 0
	knob_OAT = 2
	if OAT_is_metric == 0 then cmd_thermo_units_toggle:once() end
end


function after_physics()

	-- PREVENT ADF FREQUENCIES TO EXCEED THE RANGE OF 200-1799 kHz
	if adf1_frequency_hz < 200 then adf1_frequency_hz = 200 end
	if adf1_frequency_hz > 1799 then adf1_frequency_hz = 1799 end
	if adf1_standby_frequency_hz < 200 then adf1_standby_frequency_hz = 200 end
	if adf1_standby_frequency_hz > 1799 then adf1_standby_frequency_hz = 1799 end

	-- KEEP THE CUSTOM FLAP HANDLE SIDE SHIFT SYNC WITH THE ACTUAL XPLANE HANDLE POSITION
	if flap_ratio > 0.332 and flap_ratio < 0.334 then flap_handle_side_shift = 0 end
	if flap_ratio > 0.665 and flap_ratio < 0.667 then flap_handle_side_shift = 0.5 end
	if flap_ratio == 1 then flap_handle_side_shift = 1 end

end