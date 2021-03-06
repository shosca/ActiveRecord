// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.ActiveRecord.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Castle.ActiveRecord.Config;
    using Castle.ActiveRecord.Scopes;

    public abstract class AbstractTesting {
        /// <summary>
        /// Hook for providing the configuration before initialization
        /// </summary>
        public abstract IActiveRecordConfiguration GetConfigSource();

        /// <summary>
        /// Hook to add additional properties for each base class' configuration. As an example, "show_sql" can
        /// be added to verify the behaviour of NHibernate in specific situations.
        /// </summary>
        /// <returns>A dictionary of additional or custom properties.</returns>
        public virtual IDictionary<string, string> GetProperties()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Method that must be overridden by the test fixtures to return the assemblies
        /// that should be initialized. The stub returns an empty array.
        /// </summary>
        /// <returns></returns>
        public virtual Assembly[] GetAssemblies()
        {
            return new Assembly[0];
        }


        /// <summary>
        /// The common test setup code. To activate it in a specific test framework,
        /// it must be called from a framework-specific setup-Method.
        /// </summary>
        public virtual void SetUp()
        {
            AR.ResetInitialization();

            GetConfigSource().Initialize();

            AR.CreateSchema();
        }

        /// <summary>
        /// The common test teardown code. To activate it in a specific test framework,
        /// it must be called from a framework-specific teardown-Method.
        /// </summary>
        public virtual void TearDown()
        {
            try
            {
                AR.DisposeCurrentScope();
                AR.DropSchema();
                AR.ResetInitialization();
            }
            catch {
                
            }
        }

    }
}
