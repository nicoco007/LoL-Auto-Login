pipeline {
  agent {
    node {
      label 'windows && vs-15'
    }

  }
  stages {
    stage('Build') {
      steps {
        bat 'nuget restore'
        changeAsmVer(assemblyFile: 'LoL Auto Login\\Properties\\AssemblyInfo.cs', regexPattern: 'Assembly(\\w*)Version\\("(\\d+).(\\d+).(\\d+).(\\*)"\\)', replacementPattern: 'Assembly$1Version("$2.$3.$4.%s")', versionPattern: '$BUILD_NUMBER')
        bat 'msbuild /p:Configuration=Release /p:Platform=x86'
        bat 'iscc installer.iss'
        bat '7z a LoLAutoLogin-%GIT_BRANCH%-%BUILD_NUMBER%.zip "./LoL Auto Login/bin/x86/Release/*.dll" "./LoL Auto Login/bin/x86/Release/*.exe"'
        archiveArtifacts 'LoLAutoLogin*.zip'
        archiveArtifacts 'publish/*.exe'
      }
    }
  }
}