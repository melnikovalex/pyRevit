"""Set selected revisions on selected sheets."""

from pyrevit import revit, DB, EXEC_PARAMS
from pyrevit import forms


__doc__ = 'Select a revision from the list of revisions and '\
          'this script set that revision on all sheets in the '\
          'model as an additional revision.'\
          '\n\nShift-Click:\nAllow to change Issued Revisions'


revisions = forms.select_revisions(button_name='Select Revision',
                                   multiple=True,
                                   filterfunc=lambda rev: not rev.Issued \
                                        or EXEC_PARAMS.config_mode)

if revisions:
    sheets = forms.select_sheets(button_name='Set Revision',
                                 include_placeholder=False,
                                 use_selection=True)
    if sheets:
        with revit.Transaction('Set Revision on Sheets'):
            updated_sheets = revit.update.update_sheet_revisions(revisions,
                                                                 sheets)
        if updated_sheets:
            print('SELECTED REVISION ADDED TO THESE SHEETS:')
            print('-' * 100)
            for s in updated_sheets:
                revit.report.print_sheet(s)
