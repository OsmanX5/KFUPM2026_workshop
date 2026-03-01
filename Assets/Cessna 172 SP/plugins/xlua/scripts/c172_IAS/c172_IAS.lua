
------------------------------------------------------------------------------------------------------
--
-- THIS SCRIPT OVERRIDE THE IAS DATAREFS IN ORDER TO MATCH REAL ANEMOMETER BEHAVIOR NEAR STALL SPEED
-- REAL ONE START INDICATE LESS SPEED BELOW 70 KTS AS SHOWN IN THE TABLE BELOW:
-- CAS - IAS
-- 70  -  70
-- 60  -  57
-- 50  -  44
-- 18  -  near 0
--
------------------------------------------------------------------------------------------------------


----------------------------------- LOCATE AND/OR CREATE DATAREFS -----------------------------------
override_ias = find_dataref("sim/operation/override/override_ias") --> set to 1 allow overriding ias airspeed
cas_kts_pilot = find_dataref("sim/cockpit2/gauges/indicators/calibrated_airspeed_kts_pilot") --> read only (source airspeed from flightmodel)
ias_kts_pilot = find_dataref("sim/cockpit2/gauges/indicators/airspeed_kts_pilot") --> writable airspeed (overridden airspeed to instruments)







----------------------------------- RUNTIME CODE -----------------------------------


-- DO THIS AFTER LOADING THE ACF
function aircraft_load()
	override_ias = 1
end


-- REGULAR RUNTIME
function after_physics()

	-- UPDATE THE INSTRUMENT AIRSPEED
	-- STARTING TO CORRECT THE CAS BELOW 70 KTS
	--if cas_kts_pilot < 70 then
	--	ias_kts_pilot = cas_kts_pilot - (70 - cas_kts_pilot) * 0.3
	--else
	--	ias_kts_pilot = cas_kts_pilot
	--end

	-- UPDATE THE INSTRUMENT AIRSPEED
	-- STARTING TO MOVE AND CORRECT THE CAS ABOVE 15 AND BELOW 70 KTS
	
	if cas_kts_pilot < 15 then
		ias_kts_pilot = 0
	elseif cas_kts_pilot < 70 then
		ias_kts_pilot = (cas_kts_pilot-15)*(70/55)
	else 
		ias_kts_pilot = cas_kts_pilot
	end

end


-- DO THIS UNLOADING THE ACF
function aircraft_unload()
	override_ias = 0
end
