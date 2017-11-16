"""pyRevit root level config for all pyrevit sub-modules."""

import sys
import os
import os.path as op
import traceback


try:
    import PyRevitLoader
except ImportError:
    # this means that pyRevit is not being loaded from a pyRevit engine
    # e.g. when importing from RevitPythonShell
    PyRevitLoader = None


PYREVIT_ADDON_NAME = 'pyRevit'
VERSION_MAJOR = 4
VERSION_MINOR = 5
BUILD_METADATA = '-beta'

# ------------------------------------------------------------------------------
# config environment paths
# ------------------------------------------------------------------------------
# main pyrevit repo folder
try:
    # 3 steps back for <home>/Lib/pyrevit
    HOME_DIR = op.dirname(op.dirname(op.dirname(__file__)))
except NameError:
    raise Exception('Critical Error. Can not find home directory.')

# default extensions directory
EXTENSIONS_DEFAULT_DIR = op.join(HOME_DIR, 'extensions')

# main pyrevit lib folders
MAIN_LIB_DIR = op.join(HOME_DIR, 'pyrevitlib')
MISC_LIB_DIR = op.join(HOME_DIR, 'site-packages')

PYREVIT_MODULE_DIR = op.join(MAIN_LIB_DIR, 'pyrevit')

# loader directory
LOADER_DIR = op.join(PYREVIT_MODULE_DIR, 'loader')

# addin directory
ADDIN_DIR = op.join(LOADER_DIR, 'addin')

if PyRevitLoader:
    PYREVITLOADER_DIR = \
        op.join(ADDIN_DIR, PyRevitLoader.ScriptExecutor.EngineVersion)
    ADDIN_RESOURCE_DIR = op.join(PYREVITLOADER_DIR,
                                 'Source', 'pyRevitLoader', 'Resources')
else:
    PYREVITLOADER_DIR = ADDIN_RESOURCE_DIR = None

# add the framework dll path to the search paths
sys.path.append(ADDIN_DIR)
sys.path.append(PYREVITLOADER_DIR)


from pyrevit.framework import Process, IOException
from pyrevit.framework import Windows
from pyrevit.framework import Forms
from pyrevit.api import DB, UI

# ------------------------------------------------------------------------------
# Base Exceptions
# ------------------------------------------------------------------------------
TRACEBACK_TITLE = 'Traceback:'


# General Exceptions
class PyRevitException(Exception):
    """Base class for all pyRevit Exceptions.
    Parameters args and message are derived from Exception class.
    """
    def __str__(self):
        sys.exc_type, sys.exc_value, sys.exc_traceback = sys.exc_info()
        try:
            tb_report = traceback.format_tb(sys.exc_traceback)[0]
            if self.args:
                message = self.args[0]
                return '{}\n\n{}\n{}'.format(message,
                                             TRACEBACK_TITLE,
                                             tb_report)
            else:
                return '{}\n{}'.format(TRACEBACK_TITLE, tb_report)
        except Exception:
            # noinspection PyArgumentList
            return Exception.__str__(self)


class PyRevitIOError(PyRevitException):
    pass


# ------------------------------------------------------------------------------
# Wrapper for __revit__ builtin parameter set in scope by C# Script Executor
# ------------------------------------------------------------------------------
class _HostApplication:
    """Contains current host version and provides comparison functions."""
    def __init__(self):
        # verify __revit__
        try:
            r = __revit__
        except Exception:
            raise Exception('Critical Error: Host software is not supported. '
                            '(__revit__ handle is not available)')

    @property
    def uiapp(self):
        return __revit__

    @property
    def app(self):
        return self.uiapp.Application

    @property
    def uidoc(self):
        return getattr(self.uiapp, 'ActiveUIDocument', None)

    @property
    def doc(self):
        return getattr(self.uidoc, 'Document', None)

    @property
    def activeview(self):
        return getattr(self.uidoc, 'ActiveView', None)

    @property
    def docs(self):
        return getattr(self.app, 'Documents', None)

    @property
    def version(self):
        return self.app.VersionNumber

    @property
    def version_name(self):
        return self.app.VersionName

    @property
    def build(self):
        return self.app.VersionBuild

    @property
    def username(self):
        """Return the username from Revit API (Application.Username)"""
        uname = self.app.Username
        uname = uname.split('@')[0]  # if username is email
        # removing dots since username will be used in file naming
        uname = uname.replace('.', '')
        return uname

    @property
    def proc(self):
        return Process.GetCurrentProcess()

    @property
    def proc_id(self):
        return Process.GetCurrentProcess().Id

    @property
    def proc_name(self):
        return Process.GetCurrentProcess().ProcessName

    @property
    def proc_path(self):
        return Process.GetCurrentProcess().MainModule.FileName

    @property
    def proc_screen(self):
        return Forms.Screen.FromHandle(
            Process.GetCurrentProcess().MainWindowHandle)

    @property
    def proc_screen_workarea(self):
        screen = HOST_APP.proc_screen
        if screen:
            return screen.WorkingArea

    @property
    def proc_screen_scalefactor(self):
        screen = HOST_APP.proc_screen
        if screen:
            scaled_width = Windows.SystemParameters.PrimaryScreenWidth
            actual_wdith = screen.PrimaryScreen.WorkingArea.Width
            return abs(scaled_width / actual_wdith)

    def is_newer_than(self, version):
        return int(self.version) > int(version)

    def is_older_than(self, version):
        return int(self.version) < int(version)


HOST_APP = _HostApplication()


# ------------------------------------------------------------------------------
# Wrapper to access builtin parameters set in scope by C# Script Executor
# ------------------------------------------------------------------------------
class _ExecutorParams(object):
    def __init__(self):
        pass

    @property   # read-only
    def engine_mgr(self):
        try:
            # noinspection PyUnresolvedReferences
            return __ipyenginemanager__
        except NameError:
            raise AttributeError()

    @property   # read-only
    def engine_ver(self):
        if PyRevitLoader:
            return PyRevitLoader.ScriptExecutor.EngineVersion

    @property  # read-only
    def first_load(self):
        # if no output window is set by the executor, it means that pyRevit
        # is loading at Revit startup (not reloading)
        return True if EXEC_PARAMS.window_handle is None else False

    @property   # read-only
    def pyrevit_command(self):
        try:
            # noinspection PyUnresolvedReferences
            return __externalcommand__
        except NameError:
            return None

    @property   # read-only
    def forced_debug_mode(self):
        if self.pyrevit_command:
            return self.pyrevit_command.DebugMode
        else:
            return False

    @property   # read
    def window_handle(self):
        if self.pyrevit_command:
            return self.pyrevit_command.OutputWindow

    @property   # read-only
    def command_name(self):
        if '__commandname__' in __builtins__ \
                and __builtins__['__commandname__']:
            return __builtins__['__commandname__']
        elif self.pyrevit_command:
            return self.pyrevit_command.CommandName

    @property   # read-only
    def command_path(self):
        if '__commandpath__' in __builtins__ \
                and __builtins__['__commandpath__']:
            return __builtins__['__commandpath__']
        elif self.pyrevit_command:
            return op.dirname(self.pyrevit_command.ScriptSourceFile)

    @property
    def command_data(self):
        if self.pyrevit_command:
            return self.pyrevit_command.CommandData

    @property
    def doc_mode(self):
        try:
            # noinspection PyUnresolvedReferences
            return __sphinx__
        except NameError:
            return False

    @property
    def command_mode(self):
        return self.pyrevit_command

    @property
    def result_dict(self):
        if self.pyrevit_command:
            return self.pyrevit_command.GetResultsDictionary()


EXEC_PARAMS = _ExecutorParams()


# ------------------------------------------------------------------------------
# config user environment paths
# ------------------------------------------------------------------------------
# user env paths
USER_ROAMING_DIR = os.getenv('appdata')
USER_SYS_TEMP = os.getenv('temp')
USER_DESKTOP = op.expandvars('%userprofile%\\desktop')


if EXEC_PARAMS.doc_mode:
    PYREVIT_APP_DIR = PYREVIT_VERSION_APP_DIR = ' '
else:
    # pyrevit file directory
    PYREVIT_APP_DIR = op.join(USER_ROAMING_DIR, PYREVIT_ADDON_NAME)
    PYREVIT_VERSION_APP_DIR = op.join(PYREVIT_APP_DIR, HOST_APP.version)

for pyrvt_app_dir in [PYREVIT_APP_DIR, PYREVIT_VERSION_APP_DIR]:
    if not op.isdir(pyrvt_app_dir):
        try:
            os.mkdir(pyrvt_app_dir)
            sys.path.append(pyrvt_app_dir)
        except (OSError, IOException) as err:
            raise PyRevitException('Can not access pyRevit folder at: {} | {}'
                                   .format(pyrvt_app_dir, err))
    else:
        sys.path.append(pyrvt_app_dir)


# ------------------------------------------------------------------------------
# config template filenames
# ------------------------------------------------------------------------------
if EXEC_PARAMS.doc_mode:
    PYREVIT_FILE_PREFIX_UNIVERSAL = None
    PYREVIT_FILE_PREFIX = None
    PYREVIT_FILE_PREFIX_STAMPED = None
else:
    # pyrevit standard files prefix
    PYREVIT_FILE_PREFIX_UNIVERSAL = '{}'.format(PYREVIT_ADDON_NAME)

    PYREVIT_FILE_PREFIX = '{}_{}'.format(PYREVIT_ADDON_NAME,
                                         HOST_APP.version)

    PYREVIT_FILE_PREFIX_STAMPED = '{}_{}_{}'.format(PYREVIT_ADDON_NAME,
                                                    HOST_APP.version,
                                                    HOST_APP.proc_id)

    # pyrevit standard files prefix, with usernames
    PYREVIT_FILE_PREFIX_UNIVERSAL_USER = '{}_{}'.format(PYREVIT_ADDON_NAME,
                                                        HOST_APP.username)

    PYREVIT_FILE_PREFIX_USER = '{}_{}_{}'.format(PYREVIT_ADDON_NAME,
                                                 HOST_APP.version,
                                                 HOST_APP.username)

    PYREVIT_FILE_PREFIX_STAMPED_USER = '{}_{}_{}_{}'.format(PYREVIT_ADDON_NAME,
                                                            HOST_APP.version,
                                                            HOST_APP.username,
                                                            HOST_APP.proc_id)