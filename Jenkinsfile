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
        bat 'msbuild /p:Configuration=Release'
        bat '7z a LoLAutoLogin-%GIT_BRANCH%-%BUILD_NUMBER%.zip "./LoL Auto Login/bin/x86/Release/*.dll" "./LoL Auto Login/bin/x86/Release/*.exe"'
        archiveArtifacts 'LoLAutoLogin*.zip'
      }
    }
  }
}