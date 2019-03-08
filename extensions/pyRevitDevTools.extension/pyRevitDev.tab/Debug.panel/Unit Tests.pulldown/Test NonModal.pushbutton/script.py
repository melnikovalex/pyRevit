"""Testing non-modal windows calling actions thru external events."""
# pylint: skip-file
from pyrevit import forms
from pyrevit import UI
from pyrevit.coreutils import loadertypes


class NonModalWindow(forms.WPFWindow):
    def __init__(self, xaml_file_name, ext_event, ext_event_handler):
        forms.WPFWindow.__init__(self, xaml_file_name)
        self.ext_event = ext_event
        self.ext_event_handler = ext_event_handler

    def action(self, sender, args):
        from pyrevit import UI
        from pyrevit import forms

        if __shiftclick__:
            self.Close()
            forms.alert("Stuff")
        else:
            self.ext_event_handler.KeynoteKey = "12"
            self.ext_event_handler.KeynoteType = UI.PostableCommand.UserKeynote
            self.ext_event.Raise()

handler = loadertypes.PlaceKeynoteExternalEvent()

NonModalWindow(
    'NonModalWindow.xaml',
    ext_event=UI.ExternalEvent.Create(handler),
    ext_event_handler=handler
    ).show(modal=__shiftclick__)
