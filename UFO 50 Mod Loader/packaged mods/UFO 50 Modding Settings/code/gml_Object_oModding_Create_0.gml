scrOpenConfig();
global.saving_mode = scrReadConfig("saving_mode", 1);
global.achievements_mode = scrReadConfig("achievements_mode", 1);
scrCloseConfig();
event_user(0);