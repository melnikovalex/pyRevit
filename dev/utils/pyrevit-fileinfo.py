"""Windows exe wrapper for pyrevit revits fileinfo command."""
#pylint: disable=invalid-name
import os.path as op
import sys
import subprocess


def run_cli_process(target_file):
    """Run fileinfo command on pyRevit CLI that output the model info."""
    proc = subprocess.Popen('pyrevit revits fileinfo \"%s\"' % target_file,
                            stdout=sys.stdout, stderr=subprocess.PIPE,
                            shell=True)
    proc.wait()


# get model files from command line arguments
model_files = sys.argv[1:]
if model_files:
    for mfile in model_files:
        if op.exists(mfile):
            # do some reporting
            filename = op.basename(mfile) + ' '
            print(filename.ljust(80, '='))
            print('%s\n' % mfile)
            # now get the model info
            run_cli_process(mfile)
            print()
    input()

exit()
