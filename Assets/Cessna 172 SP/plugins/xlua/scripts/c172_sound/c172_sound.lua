-- *******************************************************************
-- Helper datarefs for use on the sound system
-- Daniela Rodríguez Careri <daniela@x-plane.com>
-- Laminar Research
-- *******************************************************************

vol_pilot_0 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_nav1")
vol_pilot_1 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_nav2")
vol_pilot_2 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_adf1")
vol_pilot_3 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_adf2")
vol_pilot_4 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_dme")
vol_pilot_5 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_dme1")
vol_pilot_6 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_dme2")
vol_pilot_7 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_mark")
vol_pilot_8 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_nav3")
vol_pilot_9 = find_dataref("sim/cockpit2/radios/actuators/audio_volume_nav4")

radio_vol_pilot = create_dataref("laminar/C172/sound/radio_volume_pilot", "array[10]")

function C172_set_radio_volumes()

    -- I hate this.
    radio_vol_pilot[0] = vol_pilot_0
    radio_vol_pilot[1] = vol_pilot_1
    radio_vol_pilot[2] = vol_pilot_2
    radio_vol_pilot[3] = vol_pilot_3
    radio_vol_pilot[4] = vol_pilot_4
    radio_vol_pilot[5] = vol_pilot_5
    radio_vol_pilot[6] = vol_pilot_6
    radio_vol_pilot[7] = vol_pilot_7
    radio_vol_pilot[8] = vol_pilot_8
    radio_vol_pilot[9] = vol_pilot_9

end


-- *******************************************************************
-- Hooks
-- *******************************************************************

function update_datarefs()
    C172_set_radio_volumes()
end

function after_physics()
    update_datarefs()
end

function after_replay()
    update_datarefs()
end
