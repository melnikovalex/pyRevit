"""Creates directory structure for a theme extension."""
#pylint: disable=import-error,invalid-name,broad-except
import os
import os.path as op

from pyrevit import revit, DB
from pyrevit import forms
from pyrevit import script
from pyrevit import script
from pyrevit.extensions import extensionmgr
from pyrevit.extensions import components as ext_cmps


__title__ = 'Create\nTheme'


# skip these bundle types
# they do not have icons and will not provide icons for sub_items
SKIP_TYPES = (ext_cmps.Extension,
              ext_cmps.Tab,
              ext_cmps.Panel,
              ext_cmps.GenericStack,
              ext_cmps.StackTwoButtonGroup,
              ext_cmps.StackThreeButtonGroup)


logger = script.get_logger()
output = script.get_output()

logger.set_quiet_mode()


extensions = extensionmgr.get_installed_ui_extensions()


def create_theme_dir(theme_path, component):
    if hasattr(component, 'unique_name') \
            and not isinstance(component, SKIP_TYPES):
        cmp_theme_dir = op.join(theme_path, component.unique_name)
        if not op.isdir(cmp_theme_dir):
            os.mkdir(cmp_theme_dir)
    if component.is_container:
        for sub_cmp in component:
            create_theme_dir(theme_path, sub_cmp)


theme_directory = forms.pick_folder(title="Select Theme Path")

selected_exts = forms.SelectFromList.show(
        extensions,
        multiselect=True,
        title="Select Extensions",
        )

if theme_directory and selected_exts:
    for ext in selected_exts:
        create_theme_dir(theme_directory, ext)


logger.reset_level()
