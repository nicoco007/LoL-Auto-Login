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
        archiveArtifacts 'LoL Auto Login/bin/Debug/**'
        cleanWs(cleanWhenAborted: true, cleanWhenFailure: true, cleanWhenNotBuilt: true, cleanWhenSuccess: true, cleanWhenUnstable: true)
      }
    }
  }
}