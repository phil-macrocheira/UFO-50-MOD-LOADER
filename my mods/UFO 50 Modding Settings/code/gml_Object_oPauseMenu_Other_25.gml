SUB_INIT = 0;
SUB_NAV = 1;
SUB_RESET = 2;

if (substate == SUB_INIT)
{
    drawMenu = true;
    scrMenuCreate("MODDING SETTINGS", 0);
    OP_SAVING = scrMenuItem(TYPE_DUAL_INT, "SAVING", global.saving_mode, 0, 2);
    OP_ACHIEVEMENT = scrMenuItem(TYPE_DUAL_INT, "ACHIEVEMENTS", global.achievements_mode, 0, 2);
    scrMenuItem(TYPE_INFO_HEADER, "INSTALLED MODS");
    mod_list_len = ds_list_size(global.mod_list);
    mod_page = 0;
    last_page = max(ceil(mod_list_len / 6), 1);
    
    if (mod_page == (last_page - 1))
    {
        page_len = mod_list_len % 7;
        OP_MODS_PAGE = scrMenuItem(TYPE_EMPTY);
    }
    else
    {
        page_len = 6;
        OP_MODS_PAGE = scrMenuItem(TYPE_DUAL_INT, "PAGE", mod_page, 1, last_page);
    }
    
    for (var i = 0; i < page_len; i++)
        ds_list_add(mod_page_list, ds_list_find_value(global.mod_list, i + (6 * mod_page)));
    
    for (var i = 0; i < 6; i++)
        scrMenuSpacer(MENU_MEDIUM_SPACER);
    
    OP_BACK = scrMenuItem(TYPE_SINGLE, scrString("menu_item_back_to_settings"));
    scrSwitchSub(SUB_NAV);
}
else if (substate == SUB_NAV)
{
    var choice = scrMenuNavigation();
    
    if (choice == -2)
    {
        scrSfxLibrary(soundSubExit[currentSoundSet]);
        scrSwitchState(statePrev);
        scrOpenConfig();
        scrWriteConfig("saving_mode", global.saving_mode);
        scrWriteConfig("achievements_mode", global.achievements_mode);
        scrCloseConfig();
        exit;
    }
    
    if (pressStart)
    {
        scrSwitchState(STATE_UNPAUSE);
        scrOpenConfig();
        scrWriteConfig("saving_mode", global.saving_mode);
        scrWriteConfig("achievements_mode", global.achievements_mode);
        scrCloseConfig();
        exit;
    }
    
    if (choice >= 0)
    {
        switch (menuSel)
        {
            case OP_SAVING:
                scrSfxLibrary(soundToggle[currentSoundSet]);
                global.saving_mode = choice;
                break;
            
            case OP_ACHIEVEMENT:
                scrSfxLibrary(soundToggle[currentSoundSet]);
                global.achievements_mode = choice;
                break;
            
            case OP_BACK:
                scrSfxLibrary(soundSubExit[currentSoundSet]);
                scrSwitchState(statePrev);
                scrOpenConfig();
                scrWriteConfig("saving_mode", global.saving_mode);
                scrWriteConfig("achievements_mode", global.achievements_mode);
                scrCloseConfig();
                break;
            
            case OP_MODS_PAGE:
                scrSfxLibrary(soundToggle[currentSoundSet]);
                mod_page = choice;
                mod_list_len = ds_list_size(global.mod_list);
                
                if (mod_page == (last_page - 1))
                    page_len = ((mod_list_len - 1) % 6) + 1;
                else
                    page_len = 6;
                
                ds_list_clear(mod_page_list);
                
                for (var i = 0; i < 6; i++)
                    ds_list_add(mod_page_list, ds_list_find_value(global.mod_list, i + (6 * mod_page)));
                
                break;
        }
    }
}