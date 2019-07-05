
DirectoryPath RepoDirectory;

bool isTaggedRelease = false;
FilePath releaseNotesPropsFile = null;
string version;

CalculateVersions();

MsBuildFlags = MsBuildFlags
    .SetVersion(version)
    .SetInformationalVersion(version);

public void CalculateVersions()
{
    DirectoryPath RepoDirectory = MakeAbsolute(GitFindRootFromPath("."));
    string lastTag = GitDescribe(RepoDirectory, false, GitDescribeStrategy.Tags, 0);

    if (string.IsNullOrEmpty(lastTag)) {
        version = "0.0.0-unknown";
        return;
    }

    bool dirty = GitHasUncommitedChanges(RepoDirectory) | GitHasStagedChanges(RepoDirectory);
    int count = GitLog(RepoDirectory, lastTag).Count();
    string branch = GitBranchCurrent(RepoDirectory).FriendlyName;

    isTaggedRelease = (count == 1);
    version = lastTag;

    if (count > 1) {
        if (branch.StartsWith("release")) {
            version += $"-LTSci{count}";
        } else {
            version += $"-ci{count}";
        }
    }

    if (dirty)
        version += $"-dirty";

    if (isTaggedRelease)
        releaseNotesPropsFile = new FilePath($"releasenotes/{lastTag}.props");
    else
        releaseNotesPropsFile = new FilePath("releasenotes/ci.props");

    releaseNotesPropsFile = MakeAbsolute(releaseNotesPropsFile);

    if (FileExists(releaseNotesPropsFile)) {
        MsBuildFlags = MsBuildFlags.WithProperty("ReleaseNotesProps", releaseNotesPropsFile.FullPath);
    } else {
        if (isTaggedRelease)
            Error($"Release notes are missing, expected release notes at: {releaseNotesPropsFile}");

        releaseNotesPropsFile = null;
    }
}
