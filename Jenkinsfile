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
        bat 'python update_build_number.py'
        bat 'msbuild /p:Configuration=Release /p:Platform="Any CPU"'
        bat 'mkdir publish'
        bat '7z a publish/LoLAutoLogin-%GIT_BRANCH%-%BUILD_NUMBER%.zip "./LoL Auto Login/bin/x86/Release/*.dll" "./LoL Auto Login/bin/x86/Release/*.exe"'
        archiveArtifacts 'publish/LoLAutoLogin-*.zip'
        bat 'iscc installer/installer.iss'
        bat '7z a publish/LoLAutoLogin-%GIT_BRANCH%-%BUILD_NUMBER%-setup.zip "./publish/*.exe"'
        archiveArtifacts 'publish/LoLAutoLogin-*-setup.zip'
      }
    }
  }
}
