"""This is the tooltip content"""

from pyrevit import forms
from pyrevit import script


slogger = script.get_logger()


# __context__ = 'zerodoc'
__context__ = ['Walls', 'Text Notes']
__helpurl__ = "https://www.youtube.com/channel/UC-0THIvKRd6n7T2a5aKYaGg"
__authors__ = ["{{author}}", "John Doe"]


if __shiftclick__:
    print('Shift-Clicked button')
    script.exit()

if __forceddebugmode__:
    print('Ctrl-Clicked button')
    script.exit()


selected_switch, switches = \
    forms.CommandSwitchWindow.show(
        ['Option_1', 'Option 2', 'Option 3', 'Option 4', 'Option 5'],
        switches=['Switch 1', 'Switch 2'],
        message='Select Option:',
        recognize_access_key=True
        )

if selected_switch:
    slogger.debug('Debug message')
    print('Try different Modifier keys with '
          'this button to check results. '
          '\n Selected Option: {}'
          '\n Switch 1 = {}'
          '\n Switch 2 = {}'.format(selected_switch,
                                    switches['Switch 1'],
                                    switches['Switch 2']))
